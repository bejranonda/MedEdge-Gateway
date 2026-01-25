using MedEdge.EdgeGateway.Models;
using MedEdge.TransformService;
using MedEdge.TransformService.Services;
using Serilog;
using System.Threading.Channels;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton(Channel.CreateUnbounded<TelemetryMessage>());

            services.Configure<MqttSubscriberOptions>(
                context.Configuration.GetSection("Mqtt"));

            services.Configure<FhirApiClientOptions>(
                context.Configuration.GetSection("FhirApi"));

            services.AddScoped<IFhirTransformService, FhirTransformService>();
            services.AddHttpClient<IFhirApiClient, FhirApiClient>();
            services.AddScoped<TelemetryToObservationMapper>();

            services.AddHostedService<MqttSubscriberService>();
            services.AddHostedService<TransformServiceBackgroundTask>();
        })
        .Build();

    Log.Information("Starting MedEdge Transform Service");
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
