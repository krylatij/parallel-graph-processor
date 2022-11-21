using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Interfaces;
using ParallelGraphProcessor.State;

namespace ParallelGraphProcessor.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly ILogger<ProcessingService> _logger;
        private readonly UploadingState _uploadingState;

        public ProcessingService(ILogger<ProcessingService> logger, UploadingState uploadingState)
        {
            _logger = logger;
            _uploadingState = uploadingState;
        }

        public async Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Processing {workItem.FullPath}.");
            
            await Task.Delay(100, cancellationToken);

            _uploadingState.Add(workItem, cancellationToken);

        }
    }
}
