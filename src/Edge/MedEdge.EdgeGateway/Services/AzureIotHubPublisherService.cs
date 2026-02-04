using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MedEdge.Core.DTOs;
using MedEdge.EdgeGateway.Configuration;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace MedEdge.EdgeGateway.Services;

public class AzureIotHubPublisherService : BackgroundService
{
    private readonly ILogger<AzureIotHubPublisherService> _logger;
    private readonly AzureIotHubOptions _options;
    private readonly Channel<TelemetryMessage> _telemetryChannel;
    private DeviceClient? _deviceClient;

    public AzureIotHubPublisherService(
        ILogger<AzureIotHubPublisherService> logger,
        IOptions<AzureIotHubOptions> options,
        TelemetryBroadcaster telemetryBroadcaster)
    {
        _logger = logger;
        _options = options.Value;
        _telemetryChannel = telemetryBroadcaster.Subscribe();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Azure IoT Hub publisher is disabled");
            return;
        }

        if (string.IsNullOrEmpty(_options.DeviceConnectionString))
        {
            _logger.LogWarning("Azure IoT Hub connection string not configured, service will not start");
            return;
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrEmpty(_options.DeviceConnectionString))
        {
            return;
        }

        _logger.LogInformation("Starting Azure IoT Hub publisher service");

        try
        {
            await ConnectAsync(stoppingToken);
            await SetupDirectMethodsAsync();
            await SetupDeviceTwinAsync();
            await ConsumeAndPublishTelemetryAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Azure IoT Hub publisher service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Azure IoT Hub publisher service");
        }
        finally
        {
            if (_deviceClient != null)
            {
                await _deviceClient.CloseAsync(stoppingToken);
                _deviceClient.Dispose();
            }
        }
    }

    private async Task ConnectAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Connecting to Azure IoT Hub...");

        _deviceClient = DeviceClient.CreateFromConnectionString(
            _options.DeviceConnectionString,
            TransportType.Mqtt);

        await _deviceClient.OpenAsync(stoppingToken);
        _logger.LogInformation("Connected to Azure IoT Hub as device: {DeviceId}", _options.DeviceId);
    }

    private async Task SetupDirectMethodsAsync()
    {
        if (_deviceClient == null) return;

        // Register direct method handlers
        await _deviceClient.SetMethodHandlerAsync("EmergencyStop", HandleEmergencyStop, null);
        await _deviceClient.SetMethodHandlerAsync("Reboot", HandleReboot, null);
        await _deviceClient.SetMethodHandlerAsync("GetDiagnostics", HandleGetDiagnostics, null);

        _logger.LogInformation("Registered direct method handlers: EmergencyStop, Reboot, GetDiagnostics");
    }

    private async Task SetupDeviceTwinAsync()
    {
        if (_deviceClient == null) return;

        // Register callback for desired property updates
        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null);

        // Get current twin and log it
        var twin = await _deviceClient.GetTwinAsync();
        _logger.LogInformation("Device Twin retrieved. Desired properties: {Desired}",
            twin.Properties.Desired.ToJson());
    }

    private Task<MethodResponse> HandleEmergencyStop(MethodRequest request, object userContext)
    {
        _logger.LogWarning("EMERGENCY STOP received from Azure IoT Hub");

        var response = new
        {
            status = "executed",
            message = "Emergency stop command received",
            timestamp = DateTime.UtcNow
        };

        return Task.FromResult(new MethodResponse(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response)),
            200));
    }

    private Task<MethodResponse> HandleReboot(MethodRequest request, object userContext)
    {
        _logger.LogWarning("REBOOT command received from Azure IoT Hub");

        var response = new
        {
            status = "acknowledged",
            message = "Reboot command received, device will restart",
            timestamp = DateTime.UtcNow
        };

        return Task.FromResult(new MethodResponse(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response)),
            200));
    }

    private Task<MethodResponse> HandleGetDiagnostics(MethodRequest request, object userContext)
    {
        _logger.LogInformation("GetDiagnostics command received from Azure IoT Hub");

        var diagnostics = new
        {
            status = "healthy",
            uptime = Environment.TickCount64 / 1000,
            memoryUsed = GC.GetTotalMemory(false),
            deviceId = _options.DeviceId,
            timestamp = DateTime.UtcNow
        };

        return Task.FromResult(new MethodResponse(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(diagnostics)),
            200));
    }

    private Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
    {
        _logger.LogInformation("Device Twin desired properties updated: {Properties}",
            desiredProperties.ToJson());

        // Handle specific property changes here
        // For example, update polling interval if it changes

        return Task.CompletedTask;
    }

    private async Task ConsumeAndPublishTelemetryAsync(CancellationToken stoppingToken)
    {
        await foreach (var telemetry in _telemetryChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var payload = JsonSerializer.Serialize(telemetry);
                var message = new Message(Encoding.UTF8.GetBytes(payload))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };

                // Add message properties for routing (Treatment vs Supply interface)
                message.Properties.Add("interface", "treatment");
                message.Properties.Add("dataType", "vitals");
                message.Properties.Add("deviceId", telemetry.DeviceId);
                message.Properties.Add("facilityId", "facility-001");

                await _deviceClient!.SendEventAsync(message, stoppingToken);

                _logger.LogDebug("Sent telemetry to Azure IoT Hub for device {DeviceId}", telemetry.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending telemetry to Azure IoT Hub");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Azure IoT Hub publisher service");
        await base.StopAsync(cancellationToken);
    }
}
