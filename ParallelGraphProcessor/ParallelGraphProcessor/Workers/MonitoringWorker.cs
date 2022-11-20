using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Monitoring;

namespace ParallelGraphProcessor.Workers
{
    public class MonitoringWorker : BackgroundService
    {
        private readonly ILogger<MonitoringWorker> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ProgressMonitor _monitor;

        public MonitoringWorker(ILogger<MonitoringWorker> logger, 
            IHostApplicationLifetime hostApplicationLifetime,
            ProgressMonitor monitor)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
            _monitor = monitor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _monitor.Refresh();

                    await Task.Delay(100, stoppingToken);
                }

                int g = 5;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run processing workers.");
                _hostApplicationLifetime.StopApplication();
            }
        }
    }
}
