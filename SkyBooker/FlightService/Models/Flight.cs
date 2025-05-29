using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FlightService.Models
{
    public class Flight
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
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
}
