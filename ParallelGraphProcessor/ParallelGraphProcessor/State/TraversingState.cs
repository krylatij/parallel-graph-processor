using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.State;

public class TraversingState : WorkState<WorkItem>
{
    public TraversingState(int queueSize, ILogger<TraversingState> logger) : base(queueSize, logger)
    {
    }
}