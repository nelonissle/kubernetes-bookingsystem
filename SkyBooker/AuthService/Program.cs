using AuthService.Data;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using System.Text;

var containerName = Environment.GetEnvironmentVariable("CONTAINER_NAME") ?? "default";

// Configure Serilog: Log both globally and per container.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Filter.ByExcluding(logEvent =>
        logEvent.Properties.ContainsKey("RequestPath") &&
        logEvent.Properties["RequestPath"].ToString().Contains("/metrics"))
    .WriteTo.Console()
    .WriteTo.File("logs/global.log", rollingInterval: RollingInterval.Infinite, shared: true)
    .WriteTo.File($"logs/{containerName}.log", rollingInterval: RollingInterval.Infinite, shared: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Ensure environment variables are added to configuration (this is included by default)
builder.Configuration.AddEnvironmentVariables();

// Use Serilog for logging
builder.Host.UseSerilog();

// Retrieve the SQLite connection string from environment variables.
var sqliteConnection = Environment.GetEnvironmentVariable("SQLITE_CONNECTION")
                        ?? throw new Exception("SQLITE_CONNECTION is not set in the environment.");
builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlite(sqliteConnection));

// Register a default admin user with a default password AUTH_ADMIN_PWD
var rootAdminPwd = Environment.GetEnvironmentVariable("AUTH_ADMIN_PWD")
                        ?? throw new Exception("AUTH_ADMIN_PWD is not set in the environment.");
builder.Services.AddDefaultAdminUser(rootAdminPwd);

// Retrieve the JWT secret key from environment variables.
var jwtKey = Environment.GetEnvironmentVariable("JWTROOTTOKEN")
                ?? throw new Exception("JWTROOTTOKEN is not set in the environment.");

builder.Services.AddSingleton<IJwtService>(sp => new JwtService(jwtKey));

// Configure JWT authentication.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Allow HTTP for local development.
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Set the URLs that the app should listen on.
builder.WebHost.UseUrls("http://+:8080");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply any pending migrations at startup.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserContext>();
    context.Database.Migrate();
}

// Enable Swagger UI only in development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable Prometheus metrics.
app.UseMetricServer();
app.UseHttpMetrics();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
