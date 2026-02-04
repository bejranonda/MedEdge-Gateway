using MedEdge.EdgeGateway.Configuration;
using MedEdge.EdgeGateway.Services;
using Serilog;

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
            // Register TelemetryBroadcaster for multi-subscriber support
            services.AddSingleton<TelemetryBroadcaster>();

            // Configure Modbus polling
            services.Configure<ModbusPollingOptions>(
                context.Configuration.GetSection("Modbus"));

            // Configure MQTT publisher
            services.Configure<MqttPublisherOptions>(
                context.Configuration.GetSection("Mqtt"));

            // Configure Azure IoT Hub publisher
            services.Configure<AzureIotHubOptions>(
                context.Configuration.GetSection("AzureIotHub"));

            // Register services
            services.AddHostedService<ModbusPollingService>();
            services.AddHostedService<MqttPublisherService>();
            services.AddHostedService<AzureIotHubPublisherService>();
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
