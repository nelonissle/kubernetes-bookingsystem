using System.Collections.Generic;
using System.Threading.Tasks;
using FlightService.Models;
using FlightService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlightService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightController : ControllerBase
    {
        private readonly IFlightRepository _repository;
        private readonly ILogger<FlightController> _logger;

        public FlightController(IFlightRepository repository, ILogger<FlightController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights()
        {
            _logger.LogInformation("GetFlights method called.");
            try
            {
                var flights = await _repository.GetFlightsAsync();
                _logger.LogInformation("Successfully retrieved {Count} flights.", flights.Count);
                _logger.LogDebug("Flights retrieved: {FlightsJson}", JsonConvert.SerializeObject(flights));
                return Ok(flights);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flights.");
                return StatusCode(500, "Internal server error");
            }
        }

        [AllowAnonymous]
        [HttpGet("{id:length(24)}", Name = "GetFlight")]
        public async Task<ActionResult<Flight>> GetFlight(string id)
        {
            _logger.LogInformation("GetFlight method called with ID: {Id}", id);
            _logger.LogDebug("Requesting flight with ID: {Id}", id);

            try
            {
                var flight = await _repository.GetFlightByIdAsync(id);
                if (flight == null)
                {
                    _logger.LogWarning("Flight with ID {Id} not found.", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved flight with ID: {Id}", id);
                _logger.LogDebug("Flight retrieved: {FlightJson}", JsonConvert.SerializeObject(flight));
                return Ok(flight);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flight with ID {Id}.", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [AllowAnonymous]
        [HttpGet("{flightId}", Name = "GetFlightByFlightId")]
        public async Task<ActionResult<Flight>> GetFlightByFlightId(string flightId)
        {
            _logger.LogInformation("GetFlightByFlightId method called with FlightId: {flightId}", flightId);
            try
            {
                var flight = await _repository.GetFlightByFlightIdAsync(flightId);
                if (flight == null)
                {
                    _logger.LogWarning("Flight with FlightId {flightId} not found.", flightId);
                    return NotFound();
                }
                _logger.LogInformation("Successfully retrieved flight with FlightId: {flightId}", flightId);
                _logger.LogDebug("Flight retrieved: {FlightJson}", JsonConvert.SerializeObject(flight));
                return Ok(flight);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving flight with FlightId {flightId}.", flightId);
                return StatusCode(500, "Internal server error");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<ActionResult> CreateFlight(Flight flight)
        {
            _logger.LogInformation("CreateFlight method called.");
            _logger.LogDebug("Incoming flight payload: {FlightPayload}", JsonConvert.SerializeObject(flight));

            if (flight == null)
            {
                _logger.LogWarning("CreateFlight method called with null flight object.");
                return BadRequest("Flight object is null");
            }

            if (string.IsNullOrWhiteSpace(flight.FlightId) ||
                string.IsNullOrWhiteSpace(flight.AirlineName) ||
                string.IsNullOrWhiteSpace(flight.Source) ||
                string.IsNullOrWhiteSpace(flight.Destination))
            {
                _logger.LogWarning("CreateFlight method called with invalid flight object: {FlightPayload}", JsonConvert.SerializeObject(flight));
                return BadRequest("Flight object is invalid. Ensure all required fields are provided and not empty.");
            }

            // Validate that the airline name is not excessively long
            if (flight.AirlineName.Length > 100)
            {
                _logger.LogWarning("CreateFlight method called with excessively long AirlineName: {AirlineName}", flight.AirlineName);
                return BadRequest("Airline name cannot exceed 100 characters.");
            }

            // Validate that the source and destination are not the same
            if (flight.Source.Equals(flight.Destination, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CreateFlight method called with the same source and destination: {Source}, {Destination}", flight.Source, flight.Destination);
                return BadRequest("Source and destination cannot be the same.");
            }

            // Validate that the departure time is earlier than the arrival time
            if (flight.Departure_Time >= flight.Arrival_Time)
            {
                _logger.LogWarning("CreateFlight method called with invalid times. Departure_Time: {Departure_Time}, Arrival_Time: {Arrival_Time}", flight.Departure_Time, flight.Arrival_Time);
                return BadRequest("Departure time must be earlier than the arrival time.");
            }

            // Validate that the departure and arrival times are in the future
            if (flight.Departure_Time <= DateTime.UtcNow || flight.Arrival_Time <= DateTime.UtcNow)
            {
                _logger.LogWarning("CreateFlight method called with past times. Departure_Time: {Departure_Time}, Arrival_Time: {Arrival_Time}", flight.Departure_Time, flight.Arrival_Time);
                return BadRequest("Departure and arrival times must be in the future.");
            }

            // Validate that available seats are non-negative
            if (flight.Available_Seats < 0)
            {
                _logger.LogWarning("CreateFlight method called with negative available seats: {Available_Seats}", flight.Available_Seats);
                return BadRequest("Available seats cannot be negative.");
            }

            // Validate that a flight with the same FlightId does not have the same destination, source, and times
            var existingFlights = await _repository.GetFlightsByFlightIdAsync(flight.FlightId);
            if (existingFlights.Any(existingFlight =>
                existingFlight.Source.Equals(flight.Source, StringComparison.OrdinalIgnoreCase) &&
                existingFlight.Destination.Equals(flight.Destination, StringComparison.OrdinalIgnoreCase) &&
                existingFlight.Departure_Time == flight.Departure_Time &&
                existingFlight.Arrival_Time == flight.Arrival_Time))
            {
                _logger.LogWarning("CreateFlight method called with duplicate flight details for FlightId: {FlightId}", flight.FlightId);
                return BadRequest("A flight with the same FlightId, source, destination, and times already exists.");
            }

            _logger.LogInformation("Processing flight creation for custom FlightId: {FlightId}", flight.FlightId);

            try
            {
                flight.Created_At = DateTime.UtcNow;
                flight.Updated_At = DateTime.UtcNow;

                await _repository.CreateFlightAsync(flight);
                _logger.LogInformation("Successfully created flight with FlightId: {FlightId}", flight.FlightId);
                _logger.LogDebug("Flight created with MongoDB Id: {MongoId}", flight.Id);
                _logger.LogDebug("Flight created: {FlightJson}", JsonConvert.SerializeObject(flight));

                return CreatedAtRoute("GetFlight", new { id = flight.Id }, flight);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a flight.");
                return StatusCode(500, "Internal server error");
            }
        }

        [AllowAnonymous]
        [HttpPut("updateSeats/{flightId}")]
        public async Task<IActionResult> UpdateFlightSeats(string flightId, [FromBody] int ticketCount)
        {
            _logger.LogInformation("UpdateFlightSeats called for FlightId: {flightId} with TicketCount: {ticketCount}", flightId, ticketCount);

            var flight = await _repository.GetFlightByFlightIdAsync(flightId);
            if (flight == null)
            {
                _logger.LogWarning("Flight with FlightId {flightId} not found.", flightId);
                return NotFound("Flight not found.");
            }

            if (flight.Available_Seats < ticketCount)
            {
                _logger.LogWarning("Not enough seats available for FlightId: {flightId}", flightId);
                return BadRequest("Not enough seats available.");
            }

            flight.Available_Seats -= ticketCount;
            flight.Updated_At = System.DateTime.UtcNow;

            await _repository.UpdateFlightAsync(flight);
            _logger.LogInformation("Successfully updated seats for FlightId: {flightId}", flightId);
            return Ok(flight);
        }
    }
}
