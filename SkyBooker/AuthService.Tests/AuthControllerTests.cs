using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AuthService.Controllers; // This gives you access to UserRegisterDto and UserLoginDto.
using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using AuthService.Models.Dtos;

namespace AuthService.Tests
{
    public class AuthControllerTests
    {
        // Create an in-memory database context for testing.
        private UserContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<UserContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid().ToString())
                .Options;
            return new UserContext(options);
        }

        // Helper method to create an AuthController instance with mocks.
        private AuthController CreateAuthController(UserContext context, IJwtService jwtService)
        {
            var loggerMock = new Mock<ILogger<AuthController>>();
            return new AuthController(context, jwtService, loggerMock.Object);
        }

        [Fact]
        public async Task Register_MissingUsernameOrPassword_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var registerDto = new UserRegisterDto
            {
                Username = "", // Missing username
                Password = "ValidPassword123$",
                EMail = "test@example.com"
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WeakPassword_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var registerDto = new UserRegisterDto
            {
                Username = "testuser",
                Password = "weakpass",  // Weak password criteria
                EMail = "test@example.com"
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_DuplicateUsername_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            // Add an existing user.
            context.Users.Add(new User
            {
                Username = "existinguser",
                Password = "dummyhash",
                EMail = "existing@example.com",
                Role = "Client"
            });
            await context.SaveChangesAsync();

            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var registerDto = new UserRegisterDto
            {
                Username = "existinguser",
                Password = "StrongPassword123$!!", // Meets criteria
                EMail = "new@example.com"
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var registerDto = new UserRegisterDto
            {
                Username = "testuser",
                Password = "StrongPassword123$!!",
                EMail = "invalid-email" // Invalid email format
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Users.Add(new User
            {
                Username = "existinguser",
                Password = "dummyhash",
                EMail = "duplicate@example.com",
                Role = "Client"
            });
            await context.SaveChangesAsync();

            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var registerDto = new UserRegisterDto
            {
                Username = "newuser",
                Password = "StrongPassword123$!!",
                EMail = "duplicate@example.com" // Duplicate email
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var registerDto = new UserRegisterDto
            {
                Username = "testuser",
                Password = "StrongPassword123$!!",
                EMail = "test@example.com",
                Role = "InvalidRole" // Invalid role
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ValidRegistration_ReturnsCreatedResultWithToken()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var jwtServiceMock = new Mock<IJwtService>();
            jwtServiceMock
                .Setup(j => j.GenerateToken(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("dummy-jwt-token");

            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var registerDto = new UserRegisterDto
            {
                Username = "newuser",
                Password = "StrongPassword123$!!", // Meets criteria
                EMail = "new@example.com",
                Role = "Client"
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var idProperty = createdResult.Value.GetType().GetProperty("id");
            Assert.NotNull(idProperty);
        }

        [Fact]
        public async Task Login_UserNotFound_ReturnsUnauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var loginDto = new UserLoginDto
            {
                Username = "nonexistent",
                Password = "SomePassword"
            };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var password = "StrongPassword123$!!";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Username = "testuser",
                Password = hashedPassword,
                EMail = "test@example.com",
                Role = "Client"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var jwtServiceMock = new Mock<IJwtService>();
            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var loginDto = new UserLoginDto
            {
                Username = "testuser",
                Password = "WrongPassword"
            };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var password = "StrongPassword123$!!";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Username = "validuser",
                Password = hashedPassword,
                EMail = "valid@example.com",
                Role = "Client"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var jwtServiceMock = new Mock<IJwtService>();
            jwtServiceMock
                .Setup(j => j.GenerateToken(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("valid-jwt-token");

            var controller = CreateAuthController(context, jwtServiceMock.Object);

            var loginDto = new UserLoginDto
            {
                Username = "validuser",
                Password = password
            };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tokenProperty = okResult.Value.GetType().GetProperty("token");
            Assert.NotNull(tokenProperty);
            var token = tokenProperty.GetValue(okResult.Value, null)?.ToString();
            Assert.Equal("valid-jwt-token", token);
        }
    }
}