using MedEdge.EdgeGateway.Models;
using MedEdge.EdgeGateway.Services;
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

            services.Configure<ModbusPollingOptions>(
                context.Configuration.GetSection("Modbus"));

            services.Configure<MqttPublisherOptions>(
                context.Configuration.GetSection("Mqtt"));

            services.AddHostedService<ModbusPollingService>();
            services.AddHostedService<MqttPublisherService>();
        })
        .Build();

    Log.Information("Starting MedEdge Edge Gateway");
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
