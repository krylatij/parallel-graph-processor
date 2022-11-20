using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.Services;

public interface IProcessingService
{
    Task ProcessAsync(WorkItem workItem);
}