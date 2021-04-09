using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Importer.Metrics
{
    public class MetricService : IHostedService
    {
        private readonly IMetricServer _server;
        private readonly ILogger<MetricService> _logger;

        public MetricService(ILogger<MetricService> logger, IMetricServer server)
        {
            _server = server;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting metric service for prometheus");
            _server.Start();
            
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping metric service");
            await _server.StopAsync();
            _server.Dispose();
        }
    }
}