// See https://aka.ms/new-console-template for more information

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParallelGraphProcessor.Configuration;
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
        x.AddScoped<IUploadingService, UploadingService>();

        x.AddSingleton<ProgressMonitor>();
        
        x.AddSingleton<TraversingState>();
        x.AddSingleton<ProcessingState>();
        x.AddSingleton<UploadingState>();

        x.AddHostedService<ProgressMonitoringWorker>();
        x.AddHostedService<TraversingWorker>();
        x.AddHostedService<ProcessingWorker>();
        x.AddHostedService<UploadingWorker>();

        x.Configure<ApplicationConfiguration>(context.Configuration.GetSection(ApplicationConfiguration.SectionName));
        x.Configure<TraversingConfiguration>(context.Configuration.GetSection(ApplicationConfiguration.SectionName).GetSection("Traversing"));
        x.Configure<ProcessingConfiguration>(context.Configuration.GetSection(ApplicationConfiguration.SectionName).GetSection("Processing"));
        x.Configure<UploadingConfiguration>(context.Configuration.GetSection(ApplicationConfiguration.SectionName).GetSection("Uploading"));
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
namespace ParallelGraphProcessor
{
    [ExcludeFromCodeCoverage]
    public partial class Program { }
}