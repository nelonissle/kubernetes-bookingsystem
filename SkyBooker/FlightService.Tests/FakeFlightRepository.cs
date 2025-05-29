using System.Collections.Generic;
using System.Threading.Tasks;
using FlightService.Models;
using FlightService.Repositories;

namespace FlightService.Tests
{
    // A fake repository that implements IFlightRepository without using MongoDB.
    public class FakeFlightRepository : IFlightRepository
    {
        private List<Flight> _flights = new List<Flight>();

        public Task<List<Flight>> GetFlightsAsync() => Task.FromResult(_flights);

        public Task<Flight?> GetFlightByIdAsync(string id) =>
            Task.FromResult(_flights.Find(f => f.Id == id));

        public Task<Flight?> GetFlightByFlightIdAsync(string flightId) =>
            Task.FromResult(_flights.Find(f => f.FlightId == flightId));

        public Task CreateFlightAsync(Flight flight)
        {
            _flights.Add(flight);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateFlightAsync(Flight flight)
        {
            var index = _flights.FindIndex(f => f.Id == flight.Id);
            if (index == -1)
                return Task.FromResult(false);
            _flights[index] = flight;
            return Task.FromResult(true);
        }

        // Helper to seed data for tests.
        public void SeedData(List<Flight> flights)
        {
            _flights = flights;
        }

        public Task<Flight?> GetFlightByNameAsync(string flightName)
        {
            return Task.FromResult(_flights.Find(f => f.AirlineName.Equals(flightName, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IEnumerable<Flight>> GetFlightsByFlightIdAsync(string flightId)
        {
            return Task.FromResult(_flights.Where(f => f.FlightId == flightId).AsEnumerable());
        }
    }
}
