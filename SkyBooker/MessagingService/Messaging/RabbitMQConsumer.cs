using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MessagingService.Messaging
{
    public class RabbitMQConsumer
    {
        private readonly string _hostname;
        private readonly string _queueName = "whatsapp-messages";
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly WhatsAppSender _whatsAppSender;
        private readonly string _destinationPhone;

        public RabbitMQConsumer(
            ILogger<RabbitMQConsumer> logger, 
            WhatsAppSender whatsAppSender)
        {
            _logger = logger;
            _whatsAppSender = whatsAppSender;

            // hostname of rabbit mq server
            _hostname = Environment.GetEnvironmentVariable("RABBITMQ_SERVICE_HOST")
                ?? throw new Exception("RABBITMQ_SERVICE_HOST is not set in the environment.");

            // Directly read from Windows environment variable
            // Throw an exception if it’s not set
            _destinationPhone = Environment.GetEnvironmentVariable("MESSAGING_DESTINATIONPHONE")
                ?? throw new Exception("MESSAGING_DESTINATIONPHONE is not set in the environment.");

            _factory = new ConnectionFactory() { HostName = _hostname };
        }

        public async Task StartListening(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting RabbitMQ Consumer...");

            try
            {
                using var connection = await _factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();
                _logger.LogInformation("Connection and channel created.");

                await channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                _logger.LogInformation("Queue '{QueueName}' declared.", _queueName);

                var consumer = new AsyncEventingBasicConsumer(channel);
                _logger.LogInformation("Consumer created.");

                consumer.ReceivedAsync += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        _logger.LogInformation("Received message: {Message}", message);

                        _whatsAppSender.SendWhatsAppMessage(_destinationPhone, "John Doe", "FL199", 2);
                        _logger.LogInformation("Message sent via WhatsApp.");
                        return Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing the received message.");
                        return Task.CompletedTask;
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: true,
                    consumer: consumer);
                _logger.LogInformation("Waiting for messages...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                }
                _logger.LogInformation("Cancellation requested, shutting down consumer...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RabbitMQ consumer.");
            }
        }
    }
}
