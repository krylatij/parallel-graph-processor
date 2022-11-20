namespace ParallelGraphProcessor.Configuration
{
    public class ApplicationConfiguration
    {
        public const string SectionName = "ApplicationConfiguration";

        public int TraversingMaxWorkers { get; set; }

        public int TraversingQueueSize { get; set; }

        public int TraversingTakeWorkTimeoutMs { get; set; }

        public string[] TraversingRoots { get; set; }

        public int ProcessingMaxWorkers { get; set; }

        public int ProcessingTakeWorkTimeoutMs { get; set; }

        public int ProcessingQueueSize { get; set; }
    }
}
