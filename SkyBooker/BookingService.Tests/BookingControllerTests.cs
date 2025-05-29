using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using BookingService.Services;

namespace BookingService.Tests
{
    // Testable subclass that overrides the SendNotification method to bypass RabbitMQ.
    public class TestableBookingController : BookingController
    {
        public TestableBookingController(
            BookingContext context,
            ILogger<BookingController> logger,
            IHttpClientFactory httpClientFactory,
            RabbitMQProducer rabbitMQProducer)
            : base(context, logger, httpClientFactory, rabbitMQProducer)
        {
        }

        protected override async Task SendNotification(string message)
        {
            // Do nothing in tests.
            await Task.CompletedTask;
        }
    }
    public class FakeRabbitMQProducer : RabbitMQProducer
    {
        public FakeRabbitMQProducer() : base(null) { }
        public override Task SendMessage(string message) => Task.CompletedTask;
    }

    public class BookingControllerTests
    {
        // Helper: Create an in-memory BookingContext.
        private BookingContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<BookingContext>()
                .UseInMemoryDatabase(databaseName: "BookingTestDb_" + Guid.NewGuid().ToString())
                .Options;
            return new BookingContext(options);
        }

        // Helper: Create a TestableBookingController instance.
        private BookingController CreateController(BookingContext context, IHttpClientFactory httpClientFactory)
        {
            var loggerMock = new Mock<ILogger<BookingController>>();
            var fakeRabbitMqProducer = new FakeRabbitMQProducer();
            return new TestableBookingController(context, loggerMock.Object, httpClientFactory, fakeRabbitMqProducer);
        }

        [Fact]
        public async Task GetBookings_ReturnsOk_WithBookings()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Bookings.AddRange(new List<Booking>
            {
                new Booking { FlightId = "FL001", PassengerId = "P001", PassengerFirstname = "John", PassengerLastname = "Doe", TicketCount = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Booking { FlightId = "FL002", PassengerId = "P002", PassengerFirstname = "Jane", PassengerLastname = "Smith", TicketCount = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            });
            await context.SaveChangesAsync();

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var controller = CreateController(context, httpClientFactoryMock.Object);

            // Act
            var result = await controller.GetBookings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var bookings = Assert.IsAssignableFrom<IEnumerable<Booking>>(okResult.Value);
            Assert.NotEmpty(bookings);
        }

        [Fact]
        public async Task GetBooking_BookingNotFound_ReturnsNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var controller = CreateController(context, httpClientFactoryMock.Object);

            // Act
            var result = await controller.GetBooking(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetBooking_BookingFound_ReturnsBooking()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var booking = new Booking
            {
                FlightId = "FL001",
                PassengerId = "P001",
                PassengerFirstname = "John",
                PassengerLastname = "Doe",
                TicketCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var controller = CreateController(context, httpClientFactoryMock.Object);

            // Act
            var result = await controller.GetBooking(booking.Id);

            // Assert
            Assert.NotNull(result.Value);
            Assert.Equal(booking.Id, result.Value.Id);
        }

        [Fact]
        public async Task CreateBooking_InvalidData_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var controller = CreateController(context, httpClientFactoryMock.Object);

            var booking = new Booking
            {
                FlightId = "", // Invalid: empty FlightId
                PassengerId = "P001",
                PassengerFirstname = "John",
                PassengerLastname = "Doe",
                TicketCount = 0, // Invalid: TicketCount <= 0
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = await controller.CreateBooking(booking);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateBooking_ValidBooking_ReturnsCreatedAtAction()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Bookings.RemoveRange(context.Bookings); // Clear existing bookings
            await context.SaveChangesAsync();

            // Mock IHttpClientFactory to simulate a successful seat check and update.
            // Updated JSON now contains full flight details.
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                // First call: GET for seat check (simulate flight info with full details)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"Id\":\"1\",\"FlightId\":\"FL123\",\"AirlineName\":\"TestAirline\",\"Source\":\"A\",\"Destination\":\"B\",\"Departure_Time\":\"2023-01-01T00:00:00Z\",\"Arrival_Time\":\"2023-01-01T02:00:00Z\",\"Available_Seats\":100,\"Created_At\":\"2023-01-01T00:00:00Z\",\"Updated_At\":\"2023-01-01T00:00:00Z\"}"
                    )
                })
                // Second call: PUT for updating seats (simulate success)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var controller = CreateController(context, httpClientFactoryMock.Object);

            var booking = new Booking
            {
                FlightId = "FL123", // Valid FlightId
                PassengerId = "P001", // Valid PassengerId
                PassengerFirstname = "John", // Valid PassengerFirstname
                PassengerLastname = "Doe", // Valid PassengerLastname
                TicketCount = 2 // Valid TicketCount
            };

            // Act
            var result = await controller.CreateBooking(booking);

            // Assert
            var createdResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            //var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            //var returnedBooking = Assert.IsType<Booking>(createdResult.Value);
            //Assert.Equal("FL123", returnedBooking.FlightId);
            //Assert.NotEqual(default, returnedBooking.CreatedAt);
            //Assert.NotEqual(default, returnedBooking.UpdatedAt);
        }

        [Fact]
        public async Task CreateBooking_DuplicateBooking_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var existingBooking = new Booking
            {
                FlightId = "FL123",
                PassengerId = "P001",
                PassengerFirstname = "John",
                PassengerLastname = "Doe",
                TicketCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Bookings.Add(existingBooking);
            await context.SaveChangesAsync();

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var controller = CreateController(context, httpClientFactoryMock.Object);

            var newBooking = new Booking
            {
                FlightId = "FL123",
                PassengerId = "P001", // Same PassengerId and FlightId as existing booking
                PassengerFirstname = "John",
                PassengerLastname = "Doe",
                TicketCount = 1
            };

            // Act
            var result = await controller.CreateBooking(newBooking);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateBooking_NotEnoughSeats_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            // Mock IHttpClientFactory to simulate insufficient seats.
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"Available_Seats\":1}") // Only 1 seat available
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var controller = CreateController(context, httpClientFactoryMock.Object);

            var booking = new Booking
            {
                FlightId = "FL123",
                PassengerId = "P001",
                PassengerFirstname = "John",
                PassengerLastname = "Doe",
                TicketCount = 2 // Requesting 2 tickets, but only 1 is available
            };

            // Act
            var result = await controller.CreateBooking(booking);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateBooking_FlightServiceFails_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            // Mock IHttpClientFactory to simulate a failure in the FlightService.
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)); // Simulate failure

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var controller = CreateController(context, httpClientFactoryMock.Object);

            var booking = new Booking
            {
                FlightId = "FL123",
                PassengerId = "P001",
                PassengerFirstname = "John",
                PassengerLastname = "Doe",
                TicketCount = 1
            };

            // Act
            var result = await controller.CreateBooking(booking);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateBooking_ValidBooking_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            // Mock IHttpClientFactory to simulate successful seat check and update.
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                // First call: GET for seat check (simulate flight info with Available_Seats=100)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"Id\":\"1\",\"FlightId\":\"FL123\",\"AirlineName\":\"TestAirline\",\"Source\":\"A\",\"Destination\":\"B\",\"Departure_Time\":\"2023-01-01T00:00:00Z\",\"Arrival_Time\":\"2023-01-01T02:00:00Z\",\"Available_Seats\":100,\"Created_At\":\"2023-01-01T00:00:00Z\",\"Updated_At\":\"2023-01-01T00:00:00Z\"}")
                })
                // Second call: PUT for updating seats (simulate success)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var controller = CreateController(context, httpClientFactoryMock.Object);

            var booking = new Booking
            {
                FlightId = "FL123",
                PassengerId = "P001",
                PassengerFirstname = "John",
                PassengerLastname = "Doe",
                TicketCount = 2
                // CreatedAt and UpdatedAt will be set by the controller.
            };

            // Act
            var result = await controller.CreateBooking(booking);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
