using BookingService.Data;
using BookingService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using System.Text;
using System.Threading.Tasks;

var containerName = Environment.GetEnvironmentVariable("CONTAINER_NAME") ?? "default";

// Configure Serilog to write logs to two fixed files, plus console
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Filter.ByExcluding(logEvent =>
        logEvent.Properties.ContainsKey("RequestPath") &&
        logEvent.Properties["RequestPath"].ToString().Contains("/metrics"))
    .WriteTo.Console()
    // Global log file: always the same file (no rolling)
    .WriteTo.File("logs/global.log", rollingInterval: RollingInterval.Infinite, shared: true)
    // Container-specific log file: always the same file (no rolling)
    .WriteTo.File($"logs/{containerName}.log", rollingInterval: RollingInterval.Infinite, shared: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Ensure that environment variables are included (this is added by default, but can be explicit)
builder.Configuration.AddEnvironmentVariables();

// Use Serilog for logging
builder.Host.UseSerilog();

var mariadb_host = Environment.GetEnvironmentVariable("MARIADB_SERVICE_HOST")
    ?? throw new Exception("MARIADB_SERVICE_HOST is not set in the environment.");
var mariadb_db = Environment.GetEnvironmentVariable("MARIADB_DATABASE")
    ?? throw new Exception("MARIADB_DATABASE is not set in the environment.");
var mariadb_user = Environment.GetEnvironmentVariable("MARIADB_USER")
    ?? throw new Exception("MARIADB_USER is not set in the environment.");
var mariadb_pwd = Environment.GetEnvironmentVariable("MARIADB_PASSWORD")
    ?? throw new Exception("MARIADB_PASSWORD is not set in the environment.");

var mariaDbConnection = $"server={mariadb_host};port=3306;database={mariadb_db};user={mariadb_user};password={mariadb_pwd};";

// Log the connection string for debugging purposes
Log.Information("Using MariaDB connection string: {ConnectionString}", mariaDbConnection);

builder.Services.AddDbContext<BookingContext>(options =>
    options.UseMySql(mariaDbConnection, ServerVersion.AutoDetect(mariaDbConnection)));

// Register the BookingDataSeeder service
builder.Services.AddTransient<BookingDataSeeder>();

// Register RabbitMQProducer service
builder.Services.AddSingleton<RabbitMQProducer>();

// Retrieve the JWT secret key from environment variables
var jwtKey = Environment.GetEnvironmentVariable("JWTROOTTOKEN")
                ?? throw new Exception("JWTROOTTOKEN is not set in the environment.");

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Attach events for detailed logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "JWT Authentication failed.");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("JWT token validated successfully. Claims: {Claims}",
                context.Principal?.Claims.Select(c => new { c.Type, c.Value }));
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT challenge triggered. Error: {Error}, ErrorDescription: {ErrorDescription}",
                context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// Log the connection stering for debugging purposes



// Register HttpClientFactory (useful for seat validation)
builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Automatically apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingContext>();
    dbContext.Database.Migrate();
}

// Run the dummy data seeder
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<BookingDataSeeder>();
    await seeder.SeedDataAsync();
}

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Enable Serilog request logging
app.UseSerilogRequestLogging();

// Enable Authentication and Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
