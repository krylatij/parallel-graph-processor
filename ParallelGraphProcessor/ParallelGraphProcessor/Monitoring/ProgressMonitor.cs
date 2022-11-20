using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.State;

namespace ParallelGraphProcessor.Monitoring
{
    public class ProgressMonitor
    {
        private readonly TraversingState _traversingState;
        private readonly Stopwatch _traversingStopwatch = new ();

        private readonly ProcessingState _processingState;
        private readonly Stopwatch _processingStopwatch = new();

        private readonly IOptions<ApplicationConfiguration> _configuration;
        private readonly ILogger<ProgressMonitor> _logger;

        public ProgressMonitor(TraversingState traversingState, 
            ProcessingState processingState,
            IOptions<ApplicationConfiguration> configuration, 
            ILogger<ProgressMonitor> logger)
        {
            _traversingState = traversingState;
            _processingState = processingState;
            _configuration = configuration;
            _logger = logger;
        }

        public void Refresh()
        {
            const int maxMessageLength = 120;

            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.DarkRed;

            if (_traversingState.IsCompleted)
            {
                if (_traversingStopwatch.IsRunning)
                {
                    _traversingStopwatch.Stop();
                }
            }
            else
            {
                _traversingStopwatch.Start();
            }

            if (_processingState.IsCompleted)
            {
                if (_processingStopwatch.IsRunning)
                {
                    _processingStopwatch.Stop();
                }
            }
            else
            {
                _processingStopwatch.Start();
            }


            var traverseMsg =
                $"Folders traversed: {_traversingState.TotalItemsProcessed}. Items in queue: {_traversingState.Count}/{_traversingState.BoundedCapacity}.";

            if (_traversingState.IsCompleted)
            {
                traverseMsg += $" Completed in '{_traversingStopwatch.Elapsed}'.";
            }
            else
            {
                traverseMsg += $"  {_configuration.Value.TraversingMaxWorkers} workers are in progress for '{_traversingStopwatch.Elapsed}'.";
            }

            var processMsg =
                $"Files processed: {_processingState.TotalItemsProcessed}. Items in queue: {_processingState.Count}/{_processingState.BoundedCapacity}.";;

            if (_processingState.IsCompleted)
            {
                processMsg += $" Completed in '{_processingStopwatch.Elapsed}'.";
            }
            else
            {
                processMsg += $"  {_configuration.Value.ProcessingMaxWorkers} workers are in progress for '{_processingStopwatch.Elapsed}'.";
            }

            Console.Write(traverseMsg.PadRight(maxMessageLength) + Environment.NewLine + processMsg.PadRight(maxMessageLength));
        }
    }
}
