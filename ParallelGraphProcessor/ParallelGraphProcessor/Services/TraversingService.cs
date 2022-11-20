using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Interfaces;
using ParallelGraphProcessor.Workers;

namespace ParallelGraphProcessor.Services
{
    public class TraversingService : ITraversingService
    {
        private readonly WorkKeeper _workKeeper;
        private readonly ILogger<TraversingService> _logger;

        public TraversingService(WorkKeeper workKeeper, ILogger<TraversingService> logger)
        {
            _workKeeper = workKeeper;
            _logger = logger;
        }

        public async Task ProcessAsync(WorkItem workItem)
        {
            _logger.LogInformation($"Traversing folder: '{workItem.FullPath}'");
            foreach (var directory in Directory.EnumerateDirectories(workItem.FullPath))
            {

                _workKeeper.TotalItemsTraversed++;

                _workKeeper.TraversingQueue.Add(new WorkItem { IsDirectory = true, FullPath = directory });
            }

            foreach (var file in Directory.EnumerateFiles(workItem.FullPath))
            {
                _workKeeper.TotalItemsTraversed++;

                await Task.Delay(50);

                _workKeeper.ProcessingQueue.Add(new WorkItem { IsDirectory = true, FullPath = file });
            }
        }
    }
}
