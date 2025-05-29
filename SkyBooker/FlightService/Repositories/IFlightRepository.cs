using FlightService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightService.Repositories
{
    public interface IFlightRepository
    {
        Task<List<Flight>> GetFlightsAsync();
        Task<Flight?> GetFlightByIdAsync(string id);
        Task<Flight?> GetFlightByFlightIdAsync(string flightId);
        Task<Flight?> GetFlightByNameAsync(string FlightId); // New method
        Task<IEnumerable<Flight>> GetFlightsByFlightIdAsync(string flightId); // Added method
        Task CreateFlightAsync(Flight flight);
        Task<bool> UpdateFlightAsync(Flight flight);
    }
}
