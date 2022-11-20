using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Interfaces;
using ParallelGraphProcessor.State;

namespace ParallelGraphProcessor.Workers;

[ExcludeFromCodeCoverage]
public class TraversingWorker : BackgroundService
{
    private readonly TraversingState _traversingState;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ApplicationConfiguration> _configuration;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public TraversingWorker(
        TraversingState traversingState,
        IServiceProvider serviceProvider,
        IOptions<ApplicationConfiguration> configuration,
        ILogger<TraversingWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _traversingState = traversingState;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Traversing process has started.");

        try
        {
            PrePopulateTraversingQueue(stoppingToken);
            
            var workers = new Task[_configuration.Value.TraversingMaxWorkers + 1];
   
            for (var i = 0; i < _configuration.Value.TraversingMaxWorkers; i++)
            {
                var worker = CreateWorker(stoppingToken);

                if (worker.Exception != null)
                {
                    foreach (var ex in worker.Exception.InnerExceptions)
                    {
                        _logger.LogError(ex, "Failed to create traversing worker.");
                    }

                    throw worker.Exception;
                }

                _logger.LogInformation("Traversing task was created.");

                workers[i] = worker;
            }

            workers[^1] = Task.Run(() => _traversingState.IsCompletedEvent.WaitOne(), stoppingToken);

            await Task.WhenAll(workers);

            _logger.LogInformation($"Traversing completed. {_traversingState.TotalItemsProcessed} files were processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run traversing workers.");
            _hostApplicationLifetime.StopApplication();
        }

        _logger.LogInformation("Traversing worker has finished.");
    }


    private void PrePopulateTraversingQueue(CancellationToken stoppingToken)
    {
        foreach (var root in _configuration.Value.TraversingRoots)
        {
            _traversingState.Add(new WorkItem { IsDirectory = true, FullPath = root }, stoppingToken);
        }
    }

    private async Task CreateWorker(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var scope = _serviceProvider.CreateScope();
        var traversingService = scope.ServiceProvider.GetRequiredService<ITraversingService>();
        while (!stoppingToken.IsCancellationRequested && !_traversingState.IsCompleted)
        {
            WorkItem workItem = null;
            try
            {
                if (_traversingState.TryTake(out workItem,
                        _configuration.Value.TraversingTakeWorkTimeoutMs,
                        stoppingToken))
                {
                    await traversingService.ProcessAsync(workItem, stoppingToken);
                    _traversingState.Commit(workItem.FullPath);
                }
            }
            catch (Exception ex)
            {
                _traversingState.Commit(workItem?.FullPath);
                _logger.LogError(ex, "Unable to process message. Exception is caught. Execution continues.");
            }
        }
    }
}
