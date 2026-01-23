using Microsoft.Extensions.Logging;
using NModbus;

namespace MedEdge.DeviceSimulator.Services;

public class ModbusServerService : BackgroundService
{
    private readonly ILogger<ModbusServerService> _logger;
    private readonly Dictionary<string, (int port, TelemetryGenerator generator)> _devices;
    private readonly Dictionary<int, IModbusSlave> _slaves = new();
    private readonly CancellationTokenSource _cts = new();

    public ModbusServerService(ILogger<ModbusServerService> logger)
    {
        _logger = logger;
        _devices = new Dictionary<string, (int port, TelemetryGenerator generator)>
        {
            { "Device-001", (502, new TelemetryGenerator()) },
            { "Device-002", (503, new TelemetryGenerator()) },
            { "Device-003", (504, new TelemetryGenerator()) }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tasks = _devices.Select(kvp => StartDeviceSimulation(kvp.Key, kvp.Value.port, kvp.Value.generator, stoppingToken));
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Modbus server service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Modbus server service");
        }
    }

    private async Task StartDeviceSimulation(string deviceId, int port, TelemetryGenerator generator, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting device simulator for {DeviceId} on port {Port}", deviceId, port);

            // Create a simple TCP slave (NModbus doesn't have built-in TCP server, so we simulate)
            // For this implementation, we'll use a simpler approach with a listener

            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            _logger.LogInformation("Modbus TCP listener started on port {Port}", port);

            var factory = new ModbusFactory();
            IModbusMaster? master = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (listener.Pending())
                    {
                        var client = await listener.AcceptTcpClientAsync(stoppingToken);
                        _logger.LogInformation("Client connected to {DeviceId}", deviceId);

                        // Simulate telemetry updates
                        _ = UpdateTelemetryLoop(deviceId, generator, stoppingToken);
                    }

                    await Task.Delay(500, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in device simulation for {DeviceId}", deviceId);
                }
            }

            listener.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting device simulation for {DeviceId}", deviceId);
        }
    }

    private async Task UpdateTelemetryLoop(string deviceId, TelemetryGenerator generator, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var telemetry = generator.GenerateTelemetry();
                _logger.LogDebug(
                    "Generated telemetry for {DeviceId}: BloodFlow={BloodFlow}, AP={AP}, VP={VP}, Temp={Temp}, Cond={Cond}",
                    deviceId,
                    telemetry.BloodFlow,
                    telemetry.ArterialPressure,
                    telemetry.VenousPressure,
                    telemetry.TemperatureDecimal / 100m,
                    telemetry.ConductivityDecimal / 100m
                );

                await Task.Delay(500, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in telemetry update loop for {DeviceId}", deviceId);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Modbus server service");
        _cts.Cancel();
        await base.StopAsync(cancellationToken);
    }
}
