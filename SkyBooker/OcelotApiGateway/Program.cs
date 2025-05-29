using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using System.Text;
using OcelotApiGateway.Handlers;
using Prometheus;
using System.Text.RegularExpressions;

var containerName = Environment.GetEnvironmentVariable("CONTAINER_NAME") ?? "default";

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Filter.ByExcluding(logEvent => 
        logEvent.Properties.ContainsKey("RequestPath") &&
        logEvent.Properties["RequestPath"].ToString().Contains("/metrics"))
    .WriteTo.Console()
    // Global log file: always the same file
    .WriteTo.File("logs/global.log", rollingInterval: RollingInterval.Infinite, shared: true)
    // Container-specific log file
    .WriteTo.File($"logs/{containerName}.log", rollingInterval: RollingInterval.Infinite, shared: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Ensure environment variables are added to configuration (this is included by default)
builder.Configuration.AddEnvironmentVariables();


// Load Ocelot configuration
// builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
// Lade ocelot.json und ersetze {{ENV_VAR}} mit echten Werten
var rawJson = File.ReadAllText("ocelot.json");

// Regex: findet {{VAR_NAME}}
var regex = new Regex("{{(.*?)}}");
var replacedJson = regex.Replace(rawJson, match =>
{
    var envVar = match.Groups[1].Value;
    return Environment.GetEnvironmentVariable(envVar) ?? throw new Exception($"Missing env var: {envVar}");
});

// Baue Konfiguration aus dem ersetzten JSON-Text
var configStream = new MemoryStream(Encoding.UTF8.GetBytes(replacedJson));
var configuration = new ConfigurationBuilder()
    .AddJsonStream(configStream)
    .Build();

// Retrieve the JWT secret key from environment variables.
var jwtKey = Environment.GetEnvironmentVariable("JWTROOTTOKEN")
                ?? throw new Exception("JWTROOTTOKEN is not set in the environment.");
                
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("JwtBearer", options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        //ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        //ClockSkew = TimeSpan.FromMinutes(2)
    };
});

// Register Ocelot services and custom delegating handlers
builder.Services.AddOcelot(configuration)
    .AddDelegatingHandler<LoggingDelegatingHandler>(global: true);

var app = builder.Build();

// Enable Prometheus metrics:
// Exposes /metrics endpoint and captures HTTP metrics
app.UseMetricServer();   // This exposes the /metrics endpoint
app.UseHttpMetrics();    // This middleware will track HTTP metrics

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// Use Ocelot Middleware
await app.UseOcelot();

app.Run();
