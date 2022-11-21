using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.State;

namespace ParallelGraphProcessor.Monitoring
{
    public class ProgressMonitor
    {
        private readonly TraversingState _traversingState;
        private readonly Stopwatch _traversingStopwatch = new ();

        private readonly ProcessingState _processingState;
        private readonly Stopwatch _processingStopwatch = new();

        private readonly UploadingState _uploadingState;
        private readonly Stopwatch _uploadingStopwatch = new();

        private readonly IOptions<ApplicationConfiguration> _configuration;
        private readonly ILogger<ProgressMonitor> _logger;

        public ProgressMonitor(TraversingState traversingState, 
            ProcessingState processingState,
            UploadingState uploadingState,
            IOptions<ApplicationConfiguration> configuration, 
            ILogger<ProgressMonitor> logger)
        {
            _traversingState = traversingState;
            _processingState = processingState;
            _uploadingState = uploadingState;
            _configuration = configuration;
            _logger = logger;
        }

        public void Refresh()
        {
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.DarkRed;


            var msg = GetWorkerMessage("Traversing", _traversingStopwatch, _traversingState, _configuration.Value.Traversing)
                      + Environment.NewLine
                      + GetWorkerMessage("Processing", _processingStopwatch, _processingState, _configuration.Value.Processing)
                      + Environment.NewLine
                      + GetWorkerMessage("Uploading", _uploadingStopwatch, _uploadingState, _configuration.Value.Uploading);

            Console.Write(msg);
        }

        private static string GetWorkerMessage(string operationName, Stopwatch watch, WorkState<WorkItem> state, WorkerConfigurationBase configuration)
        {
            const int maxMessageLength = 120;

            if (state.IsCompleted)
            {
                if (watch.IsRunning)
                {
                    watch.Stop();
                }
            }
            else
            {
                watch.Start();
            }

            var mgs =
                $"{operationName}:  Items processed: {state.TotalItemsProcessed}. Items in queue: {state.Count}/{state.BoundedCapacity}.";

            if (state.IsCompleted)
            {
                mgs += $" Completed in '{watch.Elapsed}'.";
            }
            else
            {
                mgs += $"  {configuration.MaxWorkers} workers are in progress for '{watch.Elapsed}'.";
            }

            return mgs.PadRight(maxMessageLength);
        }
    }
}
