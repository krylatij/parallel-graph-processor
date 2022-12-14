using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.Interfaces;

public interface IProcessingService
{
    Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken);
}