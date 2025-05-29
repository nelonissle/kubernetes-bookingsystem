using MessagingService.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using Prometheus; // <-- Add this

var builder = WebApplication.CreateBuilder(args);

// Get the container name from an environment variable (or use a default value)
var containerName = Environment.GetEnvironmentVariable("CONTAINER_NAME") ?? "default";

// Configure Serilog to write logs to two fixed files (global and container-specific) without rolling
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Filter.ByExcluding(logEvent => 
        logEvent.Properties.ContainsKey("RequestPath") &&
        logEvent.Properties["RequestPath"].ToString().Contains("/metrics"))
    .WriteTo.Console()
    // Global log file: always writes to the same file (no rolling), and shared among processes
    .WriteTo.File("logs/global.log", rollingInterval: RollingInterval.Infinite, shared: true)
    // Container-specific log file: always writes to the same file (no rolling)
    .WriteTo.File($"logs/{containerName}.log", rollingInterval: RollingInterval.Infinite, shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Register controllers if your service exposes HTTP endpoints
builder.Services.AddControllers();

// Register RabbitMQConsumer as a singleton and add the hosted service
builder.Services.AddSingleton<RabbitMQConsumer>();
builder.Services.AddHostedService<RabbitMQConsumerHostedService>();
builder.Services.AddSingleton<WhatsAppSender>();

var app = builder.Build();

// Enable Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Existing middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controller endpoints if any
app.MapControllers();

app.Run();
