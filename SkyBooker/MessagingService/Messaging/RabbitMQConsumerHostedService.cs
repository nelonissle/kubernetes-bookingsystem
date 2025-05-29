using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace MessagingService.Messaging
{
    public class RabbitMQConsumerHostedService : BackgroundService
    {
        private readonly ILogger<RabbitMQConsumerHostedService> _logger;
        private readonly RabbitMQConsumer _consumer;

        public RabbitMQConsumerHostedService(ILogger<RabbitMQConsumerHostedService> logger, RabbitMQConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RabbitMQ Consumer Hosted Service is running.");
            Task.Run(() => _consumer.StartListening(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }
    }
}
