using System.Collections.Generic;
using System.Threading.Tasks;
using FlightService.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FlightService.Repositories
{
    public class FlightRepository : IFlightRepository
    {
        private readonly IMongoCollection<Flight> _flights;

        public FlightRepository(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
            var database = client.GetDatabase(configuration["DatabaseName"]);
            _flights = database.GetCollection<Flight>("flights");


            // Ensure unique index on FlightId
            var indexKeys = Builders<Flight>.IndexKeys.Ascending(f => f.FlightId);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<Flight>(indexKeys, indexOptions);
            _flights.Indexes.CreateOne(indexModel);
        }

        public async Task<List<Flight>> GetFlightsAsync() =>
            await _flights.Find(f => true).ToListAsync();

        public async Task<Flight?> GetFlightByIdAsync(string id) =>
            await _flights.Find(f => f.Id == id).FirstOrDefaultAsync();

        public async Task<Flight?> GetFlightByFlightIdAsync(string flightId) =>
            await _flights.Find(f => f.FlightId == flightId).FirstOrDefaultAsync();

        public async Task<Flight?> GetFlightByNameAsync(string flightId) =>
             await _flights.Find(f => f.FlightId == flightId).FirstOrDefaultAsync();

        public async Task CreateFlightAsync(Flight flight) =>
            await _flights.InsertOneAsync(flight);

        public async Task<bool> UpdateFlightAsync(Flight flight)
        {
            var filter = Builders<Flight>.Filter.Eq(f => f.Id, flight.Id);
            var updateResult = await _flights.ReplaceOneAsync(filter, flight);
            return updateResult.IsAcknowledged && updateResult.ModifiedCount > 0;
        }

        public async Task<IEnumerable<Flight>> GetFlightsByFlightIdAsync(string flightId)
        {
            return await _flights.Find(f => f.FlightId == flightId).ToListAsync();
        }
    }
}