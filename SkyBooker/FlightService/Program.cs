using FlightService.Repositories;
using FlightService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using System.Text;
using Prometheus;
//using Serilog.Extensions.Hosting;  // Needed for DiagnosticContext
using System.Threading.Tasks;

var containerName = Environment.GetEnvironmentVariable("CONTAINER_NAME") ?? "default";

// Configure Serilog to write logs to console and files.
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

// Make sure environment variables are included in the configuration.
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog();

// Register DiagnosticContext to fix the DI error for request logging middleware.
//builder.Services.AddSingleton<DiagnosticContext>();

// Retrieve MongoDB connection string from environment variables.
var mongo_hostname = Environment.GetEnvironmentVariable("MONGO_SERVICE_HOST")
    ?? throw new Exception("MONGO_SERVICE_HOST is not set in the environment.");
// mongo password of user mongoadmin
var mongo_pwd = Environment.GetEnvironmentVariable("MONGO_PASSWORD")
    ?? throw new Exception("MONGO_PASSWORD is not set in the environment.");
// Retrieve the database name from environment variables.
var databaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE_NAME")
                ?? throw new Exception("MONGO_DATABASE_NAME is not set in the environment.");
// format mongo conneciton string: mongodb://mongoadmin:blabla@hostname:27017/FlightDb?authSource=admin
var mongoConnectionString = $"mongodb://mongoadmin:{mongo_pwd}@{mongo_hostname}:27017/{databaseName}?authSource=admin";
Log.Information("MongoDB connection string: {MongoConnectionString}", mongoConnectionString);
//var mongoConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
//                ?? throw new Exception("ConnectionStrings__MongoDb is not set in the environment.");
builder.Configuration["ConnectionStrings:MongoDb"] = mongoConnectionString;

var mongoClient = new MongoClient(mongoConnectionString);

var mongoDatabase = mongoClient.GetDatabase(databaseName);

// Register MongoDB and related services.
builder.Services.AddSingleton(mongoDatabase);
builder.Services.AddSingleton<IFlightRepository, FlightRepository>();
builder.Services.AddSingleton<DataSeeder>();

// Retrieve JWT secret key from environment variables.
var jwtKey = Environment.GetEnvironmentVariable("JWTROOTTOKEN")
                ?? throw new Exception("JWTROOTTOKEN is not set in the environment.");

// Configure JWT Authentication.
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    };

    // Attach events for detailed logging.
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger in development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable Prometheus metrics.
app.UseMetricServer();
app.UseHttpMetrics();

// Enable Serilog request logging (this now can resolve DiagnosticContext).
app.UseSerilogRequestLogging();

// Enable Authentication and Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

// Seed dummy data.
using (var scope = app.Services.CreateScope())
{
    var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await dataSeeder.SeedDataAsync();
}

app.Run();