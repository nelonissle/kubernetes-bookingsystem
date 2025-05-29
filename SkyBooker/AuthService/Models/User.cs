using System;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string EMail { get; set; }
        public string Password { get; set; } // Stored as a hash
        public string Role { get; set; } = "Client"; // Default role if none is provided
    }
}
