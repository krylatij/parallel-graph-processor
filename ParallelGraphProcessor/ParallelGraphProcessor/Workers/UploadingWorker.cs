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
public class UploadingWorker : BackgroundService
{
    private readonly UploadingState _uploadingState;
    private readonly ProcessingState _processingState;
    private readonly IOptions<UploadingConfiguration> _configuration;
    private readonly ILogger<UploadingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    
    public UploadingWorker(
        UploadingState uploadingState,
        ProcessingState processingState,
        IOptions<UploadingConfiguration> configuration,
        IServiceProvider serviceProvider,
        ILogger<UploadingWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _uploadingState = uploadingState;
        _processingState = processingState;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
       
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Uploading process has started.");
        
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
                        _logger.LogError(ex, "Failed to create uploading worker.");
                    }

                    throw worker.Exception;
                }

                _logger.LogInformation("Uploading task was created.");

                workers[i] = worker;
            }

            //processing can be completed only after processing completion
            _uploadingState.RegisterPrecondition(() => _processingState.IsCompleted);

            await Task.WhenAll(workers);

            _logger.LogInformation($"Uploading completed. {_uploadingState.TotalItemsProcessed} files were processed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run processing workers.");
            _hostApplicationLifetime.StopApplication();
        }

        _logger.LogInformation("Uploading worker has finished.");
    }

    private async Task CreateWorker(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var scope = _serviceProvider.CreateScope();
      
        var uploadingService = scope.ServiceProvider.GetRequiredService<IUploadingService>();
        while (!stoppingToken.IsCancellationRequested && !_uploadingState.IsCompleted)
        {
            try
            {
                if (_uploadingState.TryTake(out var workItem, _configuration.Value.TakeWorkTimeoutMs,
                        stoppingToken))
                {
                    await uploadingService.ProcessAsync(workItem, stoppingToken);
                    await _uploadingState.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await _uploadingState.CommitAsync();
                _logger.LogError(ex, "Unable to process message. Exception is caught. Execution continues.");
            }
        }
    }
}
