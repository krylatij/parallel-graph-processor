using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.State;

public class TraversingState : WorkState<WorkItem>
{
    public TraversingState(IOptions<TraversingConfiguration> configuration, ILogger<TraversingState> logger) : base(configuration.Value.MaxQueueSize, logger)
    {
    }
}