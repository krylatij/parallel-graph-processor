using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Interfaces;

namespace ParallelGraphProcessor.Workers;

[ExcludeFromCodeCoverage]
public class TraversingWorker : BackgroundService
{
    private readonly WorkKeeper _workKeeper;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ApplicationConfiguration> _configuration;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public TraversingWorker(
        WorkKeeper workKeeper,
        IServiceProvider serviceProvider,
        IOptions<ApplicationConfiguration> configuration,
        ILogger<TraversingWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _workKeeper = workKeeper;
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
            PrePopulateTraversingQueue();

            var traversingCompletedTokenSource = new CancellationTokenSource();
            var aggregatedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, traversingCompletedTokenSource.Token);
          
            var workers = new Task[_configuration.Value.TraversingMaxWorkers + 1];
            var completionEvents = new WaitHandle[_configuration.Value.TraversingMaxWorkers];

            for (var i = 0; i < _configuration.Value.TraversingMaxWorkers; i++)
            {
                var evt = new ManualResetEventSlim();
                var worker = CreateWorker(aggregatedCancellation.Token, evt);

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
                completionEvents[i] = evt.WaitHandle;
            }

            workers[^1] = Task.Run(() =>
            {
                do
                {
                    if (WaitHandle.WaitAll(completionEvents) && _workKeeper.TraversingQueue.Count == 0)
                    {
                        traversingCompletedTokenSource.Cancel();

                        _workKeeper.CompleteTraversing();
                    }
                } while (_workKeeper.TraversingQueue.Count > 0);

            }, stoppingToken);

            await Task.WhenAll(workers);

            _logger.LogInformation($"Traversing completed. {_workKeeper.TotalItemsTraversed} files were processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run traversing workers.");
            _hostApplicationLifetime.StopApplication();
        }

        _logger.LogInformation("Traversing worker has finished.");
    }


    private void PrePopulateTraversingQueue()
    {
        foreach (var root in _configuration.Value.TraversingRoots)
        {
            _workKeeper.TraversingQueue.Add(new WorkItem { IsDirectory = true, FullPath = root });
        }
    }

    private async Task CreateWorker(CancellationToken stoppingToken, ManualResetEventSlim evt)
    {
        await Task.Yield();

        using var scope = _serviceProvider.CreateScope();
        var traversingService = scope.ServiceProvider.GetRequiredService<ITraversingService>();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_workKeeper.TraversingQueue.TryTake(out var workItem,
                        _configuration.Value.TraversingTakeWorkTimeoutMs,
                        stoppingToken))
                {
                    evt.Reset();
                    await traversingService.ProcessAsync(workItem);
                }
                else
                {
                    evt.Set();
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to process message. Exception is caught. Execution continues.");
            }
            finally
            {
                evt.Set();
            }
        }
    }
}
