using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Interfaces;
using ParallelGraphProcessor.Services;
using ParallelGraphProcessor.State;

namespace ParallelGraphProcessor.Workers;

[ExcludeFromCodeCoverage]
public class ProcessingWorker : BackgroundService
{
    private readonly ProcessingState _processingState;
    private readonly TraversingState _traversingState;
    private readonly IOptions<ProcessingConfiguration> _configuration;
    private readonly ILogger<ProcessingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    
    public ProcessingWorker(
        ProcessingState processingState,
        TraversingState traversingState,
        IOptions<ProcessingConfiguration> configuration,
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
        _logger.LogInformation("Processing process has started.");
        
        try
        {
            var workers = new Task[_configuration.Value.MaxWorkers];
      
            for (var i = 0; i < _configuration.Value.MaxWorkers; i++)
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

                _logger.LogInformation("Processing task was created.");

                workers[i] = worker;
            }

            //processing can be completed only after traversing completion
            _processingState.RegisterPrecondition(() => _traversingState.IsCompleted);

            await Task.WhenAll(workers);

            _logger.LogInformation($"Processing completed. {_processingState.TotalItemsProcessed} files were processed.");
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
                if (_processingState.TryTake(out var workItem, _configuration.Value.TakeWorkTimeoutMs,
                        stoppingToken))
                {
                    await processingService.ProcessAsync(workItem, stoppingToken);
                    await _processingState.CommitAsync();
                }

            }
            catch (Exception ex)
            {
                await _processingState.CommitAsync();
                _logger.LogError(ex, "Unable to process message. Exception is caught. Execution continues.");
            }
        }
    }
}
