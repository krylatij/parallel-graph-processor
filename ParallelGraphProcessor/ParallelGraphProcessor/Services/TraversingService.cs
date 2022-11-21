using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Interfaces;
using ParallelGraphProcessor.State;

namespace ParallelGraphProcessor.Services
{
    public class TraversingService : ITraversingService
    {
        private readonly TraversingState _traversingState;
        private readonly ProcessingState _processingState;
        private readonly ILogger<TraversingService> _logger;

        public TraversingService(TraversingState traversingState, ProcessingState processingState, ILogger<TraversingService> logger)
        {
            _traversingState = traversingState;
            _processingState = processingState;
            _logger = logger;
        }

        public async Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Traversing folder: '{workItem.FullPath}'");
            
            foreach (var directory in Directory.EnumerateDirectories(workItem.FullPath))
            {
                _traversingState.Add(new WorkItem { IsDirectory = true, FullPath = directory }, cancellationToken);
            }

            foreach (var file in Directory.EnumerateFiles(workItem.FullPath))
            {
                _processingState.Add(new WorkItem { IsDirectory = true, FullPath = file }, cancellationToken);
                
                await Task.Delay(50);
            }
        }
    }
}
