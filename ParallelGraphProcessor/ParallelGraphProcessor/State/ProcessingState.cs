using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.State;

public class ProcessingState : WorkState<WorkItem>
{
    public ProcessingState(IOptions<ProcessingConfiguration> configuration, ILogger<ProcessingState> logger) : base(configuration.Value.MaxQueueSize, logger)
    {
    }
}