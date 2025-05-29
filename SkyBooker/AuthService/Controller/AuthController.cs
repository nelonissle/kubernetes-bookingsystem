using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Data;
using AuthService.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AuthService.Services;
using Microsoft.Extensions.Logging;
using AuthService.Models.Dtos;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly UserContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserContext context, IJwtService jwtService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        // Register as new user, public endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
        {
            _logger.LogInformation("Register endpoint called for username: {Username}", registerDto.Username);

            if (string.IsNullOrEmpty(registerDto.Username) || string.IsNullOrEmpty(registerDto.Password))
            {
                _logger.LogWarning("Registration attempt with missing username or password.");
                return BadRequest(new { message = "Username and password are required." });
            }

            if (registerDto.Username.Length < 3 || registerDto.Username.Length > 50)
            {
                _logger.LogWarning("Registration attempt with invalid username length: {Username}", registerDto.Username);
                return BadRequest(new { message = "Username must be between 3 and 50 characters long." });
            }

            if (!Regex.IsMatch(registerDto.Username, @"^[a-zA-Z0-9]+$"))
            {
                _logger.LogWarning("Registration attempt with invalid username format: {Username}", registerDto.Username);
                return BadRequest(new { message = "Username can only contain letters and numbers." });
            }

            if (string.IsNullOrEmpty(registerDto.EMail) || !Regex.IsMatch(registerDto.EMail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                _logger.LogWarning("Registration attempt with invalid email: {EMail}", registerDto.EMail);
                return BadRequest(new { message = "A valid email address is required." });
            }

            if (!IsValidPassword(registerDto.Password))
            {
                _logger.LogWarning("Weak password provided for username: {Username}", registerDto.Username);
                return BadRequest(new
                {
                    message = "Password must be at least 15 characters long and contain a letter, a number, and a special character."
                });
            }

            var weakPasswords = new[] { "password", "123456", "qwerty", "letmein", "password123" };
            if (weakPasswords.Contains(registerDto.Password.ToLower()))
            {
                _logger.LogWarning("Weak password provided for username: {Username}", registerDto.Username);
                return BadRequest(new { message = "Password is too common and insecure." });
            }

            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                _logger.LogWarning("Registration attempt for existing username: {Username}", registerDto.Username);
                return BadRequest(new { message = "Username already exists." });
            }

            if (await _context.Users.AnyAsync(u => u.EMail == registerDto.EMail))
            {
                _logger.LogWarning("Registration attempt for existing email: {EMail}", registerDto.EMail);
                return BadRequest(new { message = "Email already exists." });
            }

            var validRoles = new[] { "Admin", "Client" };
            if (!validRoles.Contains(registerDto.Role))
            {
                _logger.LogWarning("Invalid role provided for username: {Username}", registerDto.Username);
                return BadRequest(new { message = "Invalid role specified." });
            }

            string passwordHash = CreatePasswordHash(registerDto.Password);

            var user = new User
            {
                Username = registerDto.Username,
                EMail = registerDto.EMail,
                Password = passwordHash,
                Role = registerDto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Username}", registerDto.Username);

            return CreatedAtAction(nameof(Register), new { id = user.Id });
        }

        // Login as user, public endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for username: {Username}", loginDto.Username);

            // Find user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
            if (user == null || !VerifyPasswordHash(loginDto.Password, user.Password))
            {
                _logger.LogWarning("Invalid login attempt for username: {Username}", loginDto.Username);
                return Unauthorized(new { message = "Invalid username or password." });
            }

            _logger.LogInformation("User logged in successfully: {Username}", loginDto.Username);

            // Generate JWT token
            string token = _jwtService.GenerateToken(user.Username, user.Role);
            return Ok(new { token });
        }

        // --- Helper Methods ---

        private string CreatePasswordHash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        // Check for at least 15 characters, one letter, one number, and one special character.
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Password validation failed: password is null or whitespace.");
                return false;
            }
            if (password.Length < 15)
            {
                _logger.LogWarning("Password validation failed: password length is less than 15 characters.");
                return false;
            }
            if (!Regex.IsMatch(password, @"[A-Za-z]"))
            {
                _logger.LogWarning("Password validation failed: password does not contain any letters.");
                return false;
            }
            if (!Regex.IsMatch(password, @"\d"))
            {
                _logger.LogWarning("Password validation failed: password does not contain any digits.");
                return false;
            }
            if (!Regex.IsMatch(password, @"[\W_]"))
            {
                _logger.LogWarning("Password validation failed: password does not contain any special characters.");
                return false;
            }

            _logger.LogInformation("Password validation passed: password meets all criteria.");
            return true;
        }

    }

}
