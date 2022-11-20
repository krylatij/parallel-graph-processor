// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParallelGraphProcessor.Configuration;
using ParallelGraphProcessor.Entities;
using ParallelGraphProcessor.Interfaces;
using ParallelGraphProcessor.Monitoring;
using ParallelGraphProcessor.Services;
using ParallelGraphProcessor.State;
using ParallelGraphProcessor.Workers;
using Serilog;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder
    .ConfigureLogging((_, loggingBuilder) =>
    {
        loggingBuilder.ClearProviders();

        loggingBuilder.AddSerilog();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_.Configuration)
            .CreateLogger();
    })
    .UseConsoleLifetime()
    .ConfigureServices((context, x) =>
    {
        x.AddScoped<ITraversingService, TraversingService>();
        x.AddScoped<IProcessingService, ProcessingService>();

        x.AddSingleton<ProgressMonitor>();
        
        //TODO: create separate config dto
        x.AddSingleton(x =>
        {
            var opt = x.GetService<IOptions<ApplicationConfiguration>>();
            var logger = x.GetService<ILogger<TraversingState>>();

            return new TraversingState(opt.Value.TraversingQueueSize, logger);
        });

        x.AddSingleton(x =>
        {
            var opt = x.GetService<IOptions<ApplicationConfiguration>>();
            var logger = x.GetService<ILogger<ProcessingState>>();

            return new ProcessingState(opt.Value.ProcessingQueueSize, logger);
        });


        x.AddHostedService<MonitoringWorker>();
        x.AddHostedService<TraversingWorker>();
        x.AddHostedService<ProcessingWorker>();
        
        
        x.Configure<ApplicationConfiguration>(context.Configuration.GetSection(ApplicationConfiguration.SectionName));
    });

try
{
    var host = hostBuilder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
   Console.WriteLine(ex.ToString());
}
finally
{
    Console.WriteLine("Stopped web host.");
    Console.ReadLine();
}

//need this to use Program in integration tests
[ExcludeFromCodeCoverage]
public partial class Program { }