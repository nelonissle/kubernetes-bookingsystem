using BookingService.Data;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace BookingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // JWT protection
    public class BookingController : ControllerBase
    {
        private readonly BookingContext _context;

        private readonly string _gatewayHost;

        private readonly string _gatewayPort;
        private readonly ILogger<BookingController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RabbitMQProducer _rabbitMqProducer;

        public BookingController(
            BookingContext context,
            ILogger<BookingController> logger,
            IHttpClientFactory httpClientFactory,
            RabbitMQProducer rabbitMqProducer)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _rabbitMqProducer = rabbitMqProducer;

            _gatewayPort = Environment.GetEnvironmentVariable("OCELOTAPIGATEWAY_SERVICE_PORT") ?? throw new Exception("OCELOTAPIGATEWAY_SERVICE_PORT is not set in the environment.");

            _gatewayHost = Environment.GetEnvironmentVariable("OCELOTAPIGATEWAY_SERVICE_HOST") ?? throw new Exception("OCELOTAPIGATEWAY_SERVICE_HOST is not set in the environment.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            _logger.LogInformation("GetBookings GET method called.");

            try
            {
                var bookings = await _context.Bookings.ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} bookings.", bookings.Count);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Booking>> GetBooking(int id)
        {
            _logger.LogInformation("GetBooking GET method called with ID: {Id}", id);

            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking with ID {Id} not found.", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved booking with ID: {Id}", id);
                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking with ID: {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Booking>> CreateBooking(Booking booking)
        {
            _logger.LogInformation("CreateBooking POST method called.");

            // Validate incoming data
            if (string.IsNullOrEmpty(booking.FlightId) ||
                string.IsNullOrEmpty(booking.PassengerId) ||
                booking.TicketCount <= 0)
            {
                _logger.LogWarning("Invalid booking data: FlightId={FlightId}, PassengerId={PassengerId}, TicketCount={TicketCount}",
                    booking.FlightId, booking.PassengerId, booking.TicketCount);
                return BadRequest(new { message = "Invalid booking data. Check FlightId, PassengerId, and TicketCount." });
            }

            // Prevent duplicate bookings
            var existingBooking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.FlightId == booking.FlightId && b.PassengerId == booking.PassengerId);

            if (existingBooking != null)
            {
                _logger.LogWarning("Duplicate booking attempt for FlightId: {FlightId} and PassengerId: {PassengerId}",
                    booking.FlightId, booking.PassengerId);
                return BadRequest(new { message = "Duplicate booking is not allowed." });
            }

            // Enforce seat validation
            bool seatsAvailable = await CheckSeatsAsync(booking.FlightId, booking.TicketCount);
            if (!seatsAvailable)
            {
                _logger.LogWarning("Not enough seats for FlightId: {FlightId}", booking.FlightId);
                return BadRequest(new { message = "Not enough seats available." });
            }

            // Set timestamps
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Call FlightService to update available seats
                var client = _httpClientFactory.CreateClient();

                // Propagate the JWT token from the incoming request
                if (HttpContext.Request.Headers.TryGetValue("Authorization", out var token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", token.ToString());
                }

                var flightServiceUrl = $"http://{_gatewayHost}:{_gatewayPort}/flight/updateSeats/{booking.FlightId}";
                var content = new StringContent(JsonConvert.SerializeObject(booking.TicketCount), Encoding.UTF8, "application/json");
                var updateResponse = await client.PutAsync(flightServiceUrl, content);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to update seats for FlightId: {FlightId}. Status Code: {StatusCode}",
                        booking.FlightId, updateResponse.StatusCode);
                }
                else
                {
                    _logger.LogInformation("Successfully updated seats for FlightId: {FlightId}", booking.FlightId);
                }

                _logger.LogInformation("Booking created with ID: {Id} for FlightId: {FlightId}", booking.Id, booking.FlightId);

                // Send notification (in production, sends a RabbitMQ message)
                await SendNotification($"Your booking for flight {booking.FlightId} is confirmed. Booking ID: {booking.Id}.");

                return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking.");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<bool> CheckSeatsAsync(string flightId, int requestedTickets)
        {
            _logger.LogDebug("Checking seat availability for FlightId: {FlightId}", flightId);

            try
            {
                var flightServiceUrl = $"http://{_gatewayHost}:{_gatewayPort}/flight/{flightId}";
                var client = _httpClientFactory.CreateClient();

                // Propagate the JWT token from the incoming request
                if (HttpContext.Request.Headers.TryGetValue("Authorization", out var token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", token.ToString());
                }

                var response = await client.GetAsync(flightServiceUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("FlightService returned {StatusCode} for FlightId: {FlightId}",
                        response.StatusCode, flightId);
                    return false;
                }

                var flightJson = await response.Content.ReadAsStringAsync();
                var flight = System.Text.Json.JsonSerializer.Deserialize<FlightResponse>(flightJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (flight == null)
                {
                    _logger.LogWarning("Flight not found or deserialization failed for FlightId: {FlightId}", flightId);
                    return false;
                }

                _logger.LogDebug("Flight found with {AvailableSeats} seats for FlightId: {FlightId}", flight.Available_Seats, flightId);
                return flight.Available_Seats >= requestedTickets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking seats for FlightId: {FlightId}", flightId);
                return false;
            }
        }

        private class FlightResponse
        {
            public string Id { get; set; }
            public string FlightId { get; set; }
            public string AirlineName { get; set; }
            public string Source { get; set; }
            public string Destination { get; set; }
            public DateTime Departure_Time { get; set; }
            public DateTime Arrival_Time { get; set; }
            public int Available_Seats { get; set; }
            public DateTime Created_At { get; set; }
            public DateTime Updated_At { get; set; }
        }

        // Virtual method for sending notifications.
        // In production, this sends a RabbitMQ message.
        // In tests, it can be overridden to bypass the external call.
        protected virtual async Task SendNotification(string message)
        {
            await _rabbitMqProducer.SendMessage(message);
            _logger.LogInformation("WhatsApp notification message sent to RabbitMQ.");
        }
    }
}
