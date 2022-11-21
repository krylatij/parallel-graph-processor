namespace ParallelGraphProcessor.Configuration
{
    public class ApplicationConfiguration
    {
        public const string SectionName = "ApplicationConfiguration";

        public TraversingConfiguration Traversing { get; set; }

        public ProcessingConfiguration Processing { get; set; }

        public UploadingConfiguration Uploading { get; set; }
    }
}
