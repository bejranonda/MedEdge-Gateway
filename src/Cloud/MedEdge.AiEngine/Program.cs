using MedEdge.AiEngine;
using MedEdge.AiEngine.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices(services =>
        {
            services.AddScoped<IAnomalyDetector, StatisticalAnomalyDetector>();
            services.AddHostedService<AiEngineBackgroundService>();
        })
        .Build();

    Log.Information("Starting MedEdge AI Clinical Engine");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
