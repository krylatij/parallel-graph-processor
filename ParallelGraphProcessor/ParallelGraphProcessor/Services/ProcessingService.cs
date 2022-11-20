using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Workers;

namespace ParallelGraphProcessor.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly WorkKeeper _workKeeper;
        private readonly ILogger<ProcessingService> _logger;

        public ProcessingService(WorkKeeper workKeeper, ILogger<ProcessingService> logger)
        {
            _workKeeper = workKeeper;
            _logger = logger;
        }

        public async Task ProcessAsync(WorkItem workItem)
        {
            _logger.LogInformation($"Processing {workItem.FullPath}.");

            await Task.Delay(100);

            _workKeeper.TotalItemsProcessed++;
        }
    }
}
