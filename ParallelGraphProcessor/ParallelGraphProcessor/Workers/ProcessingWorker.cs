using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Services;
using ParallelGraphProcessor.State;

namespace ParallelGraphProcessor.Workers;

[ExcludeFromCodeCoverage]
public class ProcessingWorker : BackgroundService
{
    private readonly ProcessingState _processingState;
    private readonly TraversingState _traversingState;
    private readonly IOptions<ApplicationConfiguration> _configuration;
    private readonly ILogger<ProcessingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;


    public ProcessingWorker(
        ProcessingState processingState,
        TraversingState traversingState,
        IOptions<ApplicationConfiguration> configuration,
        IServiceProvider serviceProvider,
        ILogger<ProcessingWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _processingState = processingState;
        _traversingState = traversingState;
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
            var workers = new Task[_configuration.Value.ProcessingMaxWorkers + 1];
      
            for (var i = 0; i < _configuration.Value.ProcessingMaxWorkers; i++)
            {
                var worker = CreateWorker(stoppingToken);

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
            }

            //processing can be completed only after traversing completition
            _processingState.RegisterPrecondition(() => _traversingState.IsCompleted);

            workers[^1] = Task.Run(() => _processingState.IsCompletedEvent.WaitOne(), stoppingToken);

            await Task.WhenAll(workers);

            _logger.LogInformation($"Traversing completed. {_processingState.TotalItemsProcessed} files were processed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run processing workers.");
            _hostApplicationLifetime.StopApplication();
        }

        _logger.LogInformation("Processing worker has finished.");
    }

    private async Task CreateWorker(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var scope = _serviceProvider.CreateScope();
        var processingService = scope.ServiceProvider.GetRequiredService<IProcessingService>();
        while (!stoppingToken.IsCancellationRequested && !_processingState.IsCompleted)
        {
            try
            {
                if (_processingState.TryTake(out var workItem, _configuration.Value.ProcessingTakeWorkTimeoutMs,
                        stoppingToken))
                {
                    await processingService.ProcessAsync(workItem);
                    _processingState.Commit();
                }
            }
            catch (Exception ex)
            {
                _processingState.Commit();
                _logger.LogError(ex, "Unable to process message. Exception is caught. Execution continues.");
            }
        }
    }
}
