using FlightService.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightService.Services
{
    public class DataSeeder
    {
        private readonly IMongoCollection<Flight> _flightsCollection;

        public DataSeeder(IMongoDatabase database)
        {
            _flightsCollection = database.GetCollection<Flight>("flights");
        }

        public async Task SeedDataAsync()
        {
            // Check if the collection already has data
            var existingData = await _flightsCollection.Find(_ => true).ToListAsync();
            if (existingData.Count > 0)
            {
                Console.WriteLine("Dummy data already exists. Skipping seeding.");
                return;
            }

            // Create dummy data
            var dummyFlights = new List<Flight>
            {
                new Flight
                {
                    FlightId = "FL123",
                    AirlineName = "Sky Airlines",
                    Source = "New York",
                    Destination = "Los Angeles",
                    Departure_Time = DateTime.Parse("2025-10-22T10:00:00Z"),
                    Arrival_Time = DateTime.Parse("2025-10-22T14:00:00Z"),
                    Available_Seats = 150,
                    Created_At = DateTime.UtcNow,
                    Updated_At = DateTime.UtcNow
                },
                new Flight
                {
                    FlightId = "FL456",
                    AirlineName = "Oceanic Airlines",
                    Source = "San Francisco",
                    Destination = "Chicago",
                    Departure_Time = DateTime.Parse("2025-11-01T08:00:00Z"),
                    Arrival_Time = DateTime.Parse("2025-11-01T12:00:00Z"),
                    Available_Seats = 200,
                    Created_At = DateTime.UtcNow,
                    Updated_At = DateTime.UtcNow
                }
            };

            // Insert dummy data
            await _flightsCollection.InsertManyAsync(dummyFlights);
            Console.WriteLine("Dummy data seeded successfully.");
        }
    }
}