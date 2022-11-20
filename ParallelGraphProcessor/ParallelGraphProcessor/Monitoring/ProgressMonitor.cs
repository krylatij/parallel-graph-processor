using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Workers;
using ShellProgressBar;
using System;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;

namespace ParallelGraphProcessor.Monitoring
{
    public class ProgressMonitor
    {
        private readonly WorkKeeper _workKeeper;
        private readonly IOptions<ApplicationConfiguration> _configuration;
        private readonly ILogger<ProgressMonitor> _logger;

        public ProgressMonitor(WorkKeeper workKeeper, IOptions<ApplicationConfiguration> configuration, ILogger<ProgressMonitor> logger)
        {
            _workKeeper = workKeeper;
            _configuration = configuration;
            _logger = logger;
        }

        public void Refresh()
        {
            const int maxMessageLength = 120;

            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            Console.ForegroundColor = ConsoleColor.DarkRed;

            var traverseMsg =
                $"Files traversed: {_workKeeper.TotalItemsTraversed}. Items in queue: {_workKeeper.TraversingQueue.Count}/{_workKeeper.TraversingQueue.BoundedCapacity}.";

            if (_workKeeper.TraversingQueue.IsCompleted)
            {
                traverseMsg += " Completed.";
            }
            else
            {
                traverseMsg += $"  {_configuration.Value.TraversingMaxWorkers} workers are in progress.";
            }

            var processMsg =
                $"Files processed: {_workKeeper.TotalItemsProcessed}. Items in queue: {_workKeeper.ProcessingQueue.Count}/{_workKeeper.ProcessingQueue.BoundedCapacity}.";;

            if (_workKeeper.ProcessingQueue.IsCompleted)
            {
                processMsg += " Completed.";
            }
            else
            {
                processMsg += $"  {_configuration.Value.ProcessingMaxWorkers} workers are in progress.";
            }

            Console.Write(traverseMsg.PadRight(maxMessageLength) + Environment.NewLine + processMsg.PadRight(maxMessageLength));
        }
    }
}
