using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly ILogger<ProcessingService> _logger;

        public ProcessingService(ILogger<ProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task ProcessAsync(WorkItem workItem)
        {
            _logger.LogInformation($"Processing {workItem.FullPath}.");

            await Task.Delay(100);
        }
    }
}
