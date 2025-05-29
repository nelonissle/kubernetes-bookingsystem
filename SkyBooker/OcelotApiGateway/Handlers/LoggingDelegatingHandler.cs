using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OcelotApiGateway.Handlers
{
    public class LoggingDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger;

        public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Log basic request details
            _logger.LogInformation("Ocelot Request: {Method} {Uri}", request.Method, request.RequestUri);

            // Log Authorization header if available
            if (request.Headers.Contains("Authorization"))
            {
                var authHeader = string.Join(";", request.Headers.GetValues("Authorization"));
                _logger.LogInformation("Authorization header: {AuthHeader}", authHeader);
            }

            // Optionally log request content (use Debug level to avoid verbose logs in production)
            if (request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync();
                _logger.LogDebug("Request Content: {Content}", content);
            }

            // Call the next handler in the pipeline
            var response = await base.SendAsync(request, cancellationToken);

            // Log response details
            _logger.LogInformation("Ocelot Response: {StatusCode} for {Uri}", response.StatusCode, request.RequestUri);

            // Optionally log response content (use Debug level)
            if (response.Content != null)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Response Content: {ResponseContent}", responseContent);
            }

            return response;
        }
    }
}
