namespace ParallelGraphProcessor.Configuration
{
    public abstract class WorkerConfigurationBase
    {
        public int MaxWorkers { get; set; }

        public int MaxQueueSize { get; set; }

        public int TakeWorkTimeoutMs { get; set; }
    }
}
