using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.Interfaces;

public interface IUploadingService
{
    Task ProcessAsync(WorkItem workItem, CancellationToken cancellationToken);
}