using ParallelGraphProcessor.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;

namespace ParallelGraphProcessor.State
{
    public class UploadingState : WorkState<WorkItem>
    {
        public UploadingState(IOptions<UploadingConfiguration> configuration, ILogger<WorkState<WorkItem>> logger) : base(configuration.Value.MaxQueueSize, logger)
        {
        }
    }
}
