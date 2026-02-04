using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NModbus;
using NModbus.IO;
using System.Net.Sockets;
using MedEdge.Core.DTOs;
using System.Threading.Channels;

namespace MedEdge.EdgeGateway.Services;

public class ModbusPollingOptions
{
    public List<ModbusDeviceConfig> Devices { get; set; } = new();
    public int PollIntervalMs { get; set; } = 500;
    public int TimeoutMs { get; set; } = 2000;
}

public class ModbusDeviceConfig
{
    public string DeviceId { get; set; } = string.Empty;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 502;
}

public class ModbusPollingService : BackgroundService
{
    private readonly ILogger<ModbusPollingService> _logger;
    private readonly ModbusPollingOptions _options;
    private readonly TelemetryBroadcaster _telemetryBroadcaster;
    private readonly IModbusFactory _modbusFactory;

    public ModbusPollingService(
        ILogger<ModbusPollingService> logger,
        IOptions<ModbusPollingOptions> options,
        TelemetryBroadcaster telemetryBroadcaster)
    {
        _logger = logger;
        _options = options.Value;
        _telemetryBroadcaster = telemetryBroadcaster;
        _modbusFactory = new ModbusFactory();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Modbus polling service with {DeviceCount} devices", _options.Devices.Count);

        var pollTasks = _options.Devices.Select(device =>
            PollDeviceAsync(device, stoppingToken)
        ).ToList();

        try
        {
            await Task.WhenAll(pollTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Modbus polling service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Modbus polling service");
        }
    }

    private async Task PollDeviceAsync(ModbusDeviceConfig deviceConfig, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling for device {DeviceId} on {Host}:{Port}",
            deviceConfig.DeviceId, deviceConfig.Host, deviceConfig.Port);

        TcpClient? client = null;
        IModbusMaster? master = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Connect if not already connected
                if (client == null || !client.Connected)
                {
                    client = new TcpClient
                    {
                        ReceiveTimeout = _options.TimeoutMs,
                        SendTimeout = _options.TimeoutMs
                    };
                    await client.ConnectAsync(deviceConfig.Host, deviceConfig.Port, stoppingToken);
                    var factory = new ModbusFactory();
                    master = factory.CreateMaster(client);
                    _logger.LogInformation("Connected to device {DeviceId}", deviceConfig.DeviceId);
                }

                // Read holding registers (blood flow, pressures, temperature, etc.)
                if (master != null)
                {
                    var registers = await master.ReadHoldingRegistersAsync(1, 0, 12);

                    var bloodFlow = registers[0];
                    var arterialPressure = registers[2];
                    var venousPressure = registers[4];
                    var temperatureRaw = registers[6];
                    var conductivityRaw = registers[8];
                    var treatmentTime = registers[10];

                    var temperature = temperatureRaw / 100.0;
                    var conductivity = conductivityRaw / 100.0;

                    var message = new TelemetryMessage(
                        deviceConfig.DeviceId,
                        DateTime.UtcNow,
                        new Dictionary<string, double>
                        {
                            { "bloodFlow", bloodFlow },
                            { "arterialPressure", arterialPressure },
                            { "venousPressure", venousPressure },
                            { "dialysateTemperature", temperature },
                            { "conductivity", conductivity },
                            { "treatmentTime", treatmentTime }
                        },
                        new Dictionary<string, bool>
                        {
                            { "pressureLow", arterialPressure < 80 },
                            { "pressureHigh", venousPressure > 200 }
                        }
                    );

                    await _telemetryBroadcaster.BroadcastAsync(message, stoppingToken);

                    _logger.LogDebug(
                        "Polled {DeviceId}: BF={BF}, AP={AP}, VP={VP}, Temp={Temp}, Cond={Cond}",
                        deviceConfig.DeviceId,
                        bloodFlow,
                        arterialPressure,
                        venousPressure,
                        temperature,
                        conductivity
                    );
                }

                await Task.Delay(_options.PollIntervalMs, stoppingToken);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to connect to device {DeviceId}, retrying...", deviceConfig.DeviceId);
                client?.Dispose();
                client = null;
                master = null;
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling device {DeviceId}", deviceConfig.DeviceId);
                client?.Dispose();
                client = null;
                master = null;
                await Task.Delay(1000, stoppingToken);
            }
        }

        client?.Dispose();
    }
}
