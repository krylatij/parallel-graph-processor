using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Entities;

namespace ParallelGraphProcessor.State;

public class ProcessingState : WorkState<WorkItem>
{
    public ProcessingState(int queueSize, ILogger<ProcessingState> logger) : base(queueSize, logger)
    {
    }
}