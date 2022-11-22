using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Interfaces;

namespace ParallelGraphProcessor.Services
{
    public class UploadingService : IUploadingService
    {
        private readonly ILogger<UploadingService> _logger;

        public UploadingService(ILogger<UploadingService> logger)
        {
            _logger = logger;
        }

        public async Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Uploading {workItem.FullPath}.");

            await Task.Delay(1_00);
        }
    }
}
