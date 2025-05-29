using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingService.Services
{
    public class BookingDataSeeder
    {
        private readonly BookingContext _context;

        public BookingDataSeeder(BookingContext context)
        {
            _context = context;
        }

        public async Task SeedDataAsync()
        {
            // Check if any bookings already exist
            var existingBookings = await _context.Bookings.ToListAsync();
            if (existingBookings.Count > 0)
            {
                Console.WriteLine("Dummy data already exists. Skipping seeding.");
                return;
            }

            // Create dummy bookings
            var dummyBookings = new List<Booking>
            {
                new Booking
                {
                    FlightId = "FL199",
                    PassengerId = "P001",
                    PassengerFirstname = "John",
                    PassengerLastname = "Doe",
                    TicketCount = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Booking
                {
                    FlightId = "FL199",
                    PassengerId = "P002",
                    PassengerFirstname = "Jane",
                    PassengerLastname = "Smith",
                    TicketCount = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Insert the dummy bookings into the database
            _context.Bookings.AddRange(dummyBookings);
            await _context.SaveChangesAsync();

            Console.WriteLine("Dummy data seeded successfully.");
        }
    }
}
