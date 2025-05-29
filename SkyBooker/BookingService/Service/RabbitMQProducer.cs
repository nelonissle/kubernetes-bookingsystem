using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Services
{
    public class RabbitMQProducer
    {
        private readonly string _hostname;
        private readonly string _queueName = "whatsapp-messages";
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMQProducer>? _logger;

        public RabbitMQProducer()
        {
            // hostname of rabbit mq server
            _hostname = Environment.GetEnvironmentVariable("RABBITMQ_SERVICE_HOST")
                ?? throw new Exception("RABBITMQ_SERVICE_HOST is not set in the environment.");

            _factory = new ConnectionFactory() { HostName = _hostname };
        }

        // Add this constructor for testing/mocking
        public RabbitMQProducer(ILogger<RabbitMQProducer>? logger)
            : this()
        {
            _logger = logger;
        }

        public virtual async Task SendMessage(string message)
        {
            using (var connection = await _factory.CreateConnectionAsync())
            using (var channel = await connection.CreateChannelAsync())
            {
                await channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _queueName,
                    body: body);
            }
        }
    }
}
