using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightService.Controllers;
using FlightService.Models;
using FlightService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlightService.Tests
{
    public class FlightControllerTests
    {
        // Helper: Create a FlightController instance using the fake repository.
        private FlightController CreateController(FakeFlightRepository repository)
        {
            var loggerMock = new Mock<ILogger<FlightController>>();
            return new FlightController(repository, loggerMock.Object);
        }

        [Fact]
        public async Task GetFlights_ReturnsOk_WithFlights()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            fakeRepo.SeedData(new List<Flight>
            {
                new Flight { Id = "1", FlightId = "FL123", AirlineName = "AirTest", Source = "CityA", Destination = "CityB", Departure_Time = DateTime.UtcNow, Arrival_Time = DateTime.UtcNow.AddHours(2), Available_Seats = 100 },
                new Flight { Id = "2", FlightId = "FL456", AirlineName = "AirTest", Source = "CityC", Destination = "CityD", Departure_Time = DateTime.UtcNow, Arrival_Time = DateTime.UtcNow.AddHours(3), Available_Seats = 150 }
            });
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.GetFlights();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var flights = Assert.IsAssignableFrom<IEnumerable<Flight>>(okResult.Value);
            Assert.NotEmpty(flights);
        }

        [Fact]
        public async Task GetFlight_FlightNotFound_ReturnsNotFound()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.GetFlight("nonexistent");

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetFlight_FlightFound_ReturnsOkWithFlight()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var flight = new Flight { Id = "1", FlightId = "FL123", AirlineName = "AirTest", Source = "CityA", Destination = "CityB", Departure_Time = DateTime.UtcNow, Arrival_Time = DateTime.UtcNow.AddHours(2), Available_Seats = 100 };
            fakeRepo.SeedData(new List<Flight> { flight });
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.GetFlight("1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFlight = Assert.IsType<Flight>(okResult.Value);
            Assert.Equal("FL123", returnedFlight.FlightId);
        }

        [Fact]
        public async Task GetFlightByFlightId_FlightNotFound_ReturnsNotFound()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.GetFlightByFlightId("NONEXISTENT");

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetFlightByFlightId_FlightFound_ReturnsOkWithFlight()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var flight = new Flight { Id = "1", FlightId = "FL123", AirlineName = "AirTest", Source = "CityA", Destination = "CityB", Departure_Time = DateTime.UtcNow, Arrival_Time = DateTime.UtcNow.AddHours(2), Available_Seats = 100 };
            fakeRepo.SeedData(new List<Flight> { flight });
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.GetFlightByFlightId("FL123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFlight = Assert.IsType<Flight>(okResult.Value);
            Assert.Equal("AirTest", returnedFlight.AirlineName);
        }

        [Fact]
        public async Task CreateFlight_InvalidFlight_ReturnsBadRequest()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var controller = CreateController(fakeRepo);
            var flight = new Flight { FlightId = "", AirlineName = "", Source = "", Destination = "" };

            // Act
            var result = await controller.CreateFlight(flight);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateFlight_ValidFlight_ReturnsCreatedAtRoute()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var controller = CreateController(fakeRepo);
            var flight = new Flight
            {
                FlightId = "FL123",
                AirlineName = "AirTest",
                Source = "CityA",
                Destination = "CityB",
                Departure_Time = DateTime.UtcNow.AddHours(1),
                Arrival_Time = DateTime.UtcNow.AddHours(3),
                Available_Seats = 100
            };

            // Act
            var result = await controller.CreateFlight(flight);

            // Assert
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result);
            var returnedFlight = Assert.IsType<Flight>(createdResult.Value);
            Assert.Equal("FL123", returnedFlight.FlightId);
        }

        [Fact]
        public async Task UpdateFlightSeats_FlightNotFound_ReturnsNotFound()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.UpdateFlightSeats("NONEXISTENT", 5);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateFlightSeats_NotEnoughSeats_ReturnsBadRequest()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var flight = new Flight { Id = "1", FlightId = "FL123", AirlineName = "AirTest", Source = "CityA", Destination = "CityB", Departure_Time = DateTime.UtcNow, Arrival_Time = DateTime.UtcNow.AddHours(2), Available_Seats = 3 };
            fakeRepo.SeedData(new List<Flight> { flight });
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.UpdateFlightSeats("FL123", 5);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateFlightSeats_ValidUpdate_ReturnsOkWithFlight()
        {
            // Arrange
            var fakeRepo = new FakeFlightRepository();
            var flight = new Flight { Id = "1", FlightId = "FL123", AirlineName = "AirTest", Source = "CityA", Destination = "CityB", Departure_Time = DateTime.UtcNow, Arrival_Time = DateTime.UtcNow.AddHours(2), Available_Seats = 100 };
            fakeRepo.SeedData(new List<Flight> { flight });
            var controller = CreateController(fakeRepo);

            // Act
            var result = await controller.UpdateFlightSeats("FL123", 5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedFlight = Assert.IsType<Flight>(okResult.Value);
            Assert.Equal(95, updatedFlight.Available_Seats);
        }
    }
}