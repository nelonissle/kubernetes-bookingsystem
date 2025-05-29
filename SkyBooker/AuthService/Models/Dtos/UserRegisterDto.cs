// filepath: f:\repos\Modul_321_PraxisArbeit\SkyBooker\AuthService\Models\Dtos\UserRegisterDto.cs
namespace AuthService.Models.Dtos
{
    public class UserRegisterDto
    {
        public string Username { get; set; }
        public string EMail { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "Client";
    }
}