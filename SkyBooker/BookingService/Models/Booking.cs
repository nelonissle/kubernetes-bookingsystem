using System;
using System.ComponentModel.DataAnnotations;

namespace BookingService.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }
        public string FlightId { get; set; }
        public string PassengerId { get; set; }
        public string PassengerFirstname { get; set; }
        public string PassengerLastname { get; set; }
        public int TicketCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
