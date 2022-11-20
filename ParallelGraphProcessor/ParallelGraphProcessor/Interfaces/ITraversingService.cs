using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.Interfaces;

public interface ITraversingService
{
    Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken);
}