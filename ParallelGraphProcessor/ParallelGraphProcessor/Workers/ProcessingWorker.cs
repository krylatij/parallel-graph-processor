using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Services;

namespace ParallelGraphProcessor.Workers;

[ExcludeFromCodeCoverage]
public class ProcessingWorker : BackgroundService
{
    private readonly WorkKeeper _workKeeper;
    private readonly IOptions<ApplicationConfiguration> _configuration;
    private readonly ILogger<ProcessingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;


    public ProcessingWorker(
        WorkKeeper workKeeper,
        IOptions<ApplicationConfiguration> configuration,
        IServiceProvider serviceProvider,
        ILogger<ProcessingWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _workKeeper = workKeeper;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
       
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Traversing process has started.");
        
        try
        {
            var processingCompletedTokenSource = new CancellationTokenSource();
            
            var aggregatedTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, processingCompletedTokenSource.Token);

            var workers = new Task[_configuration.Value.ProcessingMaxWorkers + 1];
            var completionEvents = new WaitHandle[_configuration.Value.ProcessingMaxWorkers + 1];

            for (var i = 0; i < _configuration.Value.ProcessingMaxWorkers; i++)
            {
                var evt = new ManualResetEventSlim();
                var worker = CreateWorker(aggregatedTokenSource.Token, evt);

                if (worker.Exception != null)
                {
                    foreach (var ex in worker.Exception.InnerExceptions)
                    {
                        _logger.LogError(ex, "Failed to create processing worker.");
                    }

                    throw worker.Exception;
                }

                _logger.LogInformation("Traversing task was created.");

                workers[i] = worker;
                completionEvents[i] = evt.WaitHandle;
            }

            completionEvents[^1] = _workKeeper.TraversingCompletedEvent.WaitHandle;

            workers[^1] = Task.Run(() =>
            {
                if (WaitHandle.WaitAll(completionEvents))
                {
                    processingCompletedTokenSource.Cancel();

                    _workKeeper.ProcessingQueue.CompleteAdding();
                }
            }, stoppingToken);

            await Task.WhenAll(workers);

            _logger.LogInformation($"Traversing completed. {_workKeeper.TotalItemsProcessed} files were processed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run processing workers.");
            _hostApplicationLifetime.StopApplication();
        }

        _logger.LogInformation("Processing worker has finished.");
    }

    private async Task CreateWorker(CancellationToken stoppingToken, ManualResetEventSlim evt)
    {
        await Task.Yield();

        using var scope = _serviceProvider.CreateScope();
        var processingService = scope.ServiceProvider.GetRequiredService<IProcessingService>();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_workKeeper.ProcessingQueue.TryTake(out var workItem, _configuration.Value.ProcessingTakeWorkTimeoutMs,
                        stoppingToken))
                {
                    evt.Reset();
                    await processingService.ProcessAsync(workItem);
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
