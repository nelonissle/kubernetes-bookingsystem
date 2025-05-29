using Xunit;
using AuthService.Services;
using System.Security.Claims;

namespace AuthService.Tests
{
    public class JwtServiceTests
    {
        // Use a sufficiently long secret key for testing.
        private readonly string secretKey = "this_is_a_very_secure_key_1234567890";

        [Fact]
        public void GenerateToken_ValidInputs_ReturnsToken()
        {
            // Arrange
            var jwtService = new JwtService(secretKey);
            var username = "testuser";
            var role = "Client";

            // Act
            var token = jwtService.GenerateToken(username, role);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public void ValidateToken_ValidToken_ReturnsClaimsPrincipal()
        {
            // Arrange
            var jwtService = new JwtService(secretKey);
            var username = "testuser";
            var role = "Client";
            var token = jwtService.GenerateToken(username, role);

            // Act
            var principal = jwtService.ValidateToken(token);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal(username, principal.Identity.Name);
            var roleClaim = principal.FindFirst(ClaimTypes.Role);
            Assert.NotNull(roleClaim);
            Assert.Equal(role, roleClaim.Value);
        }

        [Fact]
        public void ValidateToken_InvalidToken_ReturnsNull()
        {
            // Arrange
            var jwtService = new JwtService(secretKey);
            var invalidToken = "invalid.token.value";

            // Act
            var principal = jwtService.ValidateToken(invalidToken);

            // Assert
            Assert.Null(principal);
        }
    }
}
