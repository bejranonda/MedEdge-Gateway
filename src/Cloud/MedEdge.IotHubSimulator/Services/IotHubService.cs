using MedEdge.IotHubSimulator.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace MedEdge.IotHubSimulator.Services;

/// <summary>
/// Simulated Azure IoT Hub Core Service
/// Demonstrates: Device Registry, Twins, Direct Methods, Telemetry ingestion
/// Maps to medical device requirements: Device identity, configuration, control
/// </summary>
public class IotHubService
{
    private readonly ConcurrentDictionary<string, Device> _devices = new();
    private readonly ConcurrentQueue<TelemetryMessage> _telemetryBuffer = new();
    private readonly ILogger<IotHubService> _logger;

    public IotHubService(ILogger<IotHubService> logger)
    {
        _logger = logger;

        // Initialize with sample B. Braun dialysis machines
        InitializeSampleDevices();
    }

    private void InitializeSampleDevices()
    {
        // Legacy dialysis machine (older model, needs TPM retrofit)
        var legacyDevice = new Device
        {
            DeviceId = "medical-dialysis-legacy-001",
            DeviceKey = GenerateDeviceKey(),
            Status = DeviceStatus.Enabled,
            ConnectionState = DeviceConnectionState.Disconnected,
            TemplateId = "dialysis-legacy-v1",
            Tags = new Dictionary<string, string>
            {
                ["manufacturer"] = "MedTech Corp",
                ["model"] = "Dialysis Pro+",
                ["type"] = "dialysis",
                ["location"] = "Berlin-Center-01",
                ["firmware"] = "4.2.1"
            },
            Twin = new DeviceTwin
            {
                Properties = new TwinProperties
                {
                    Desired = new Dictionary<string, object>
                    {
                        ["pollingInterval"] = 500,
                        ["maxBloodFlow"] = 450,
                        ["safetyEnabled"] = true
                    },
                    Reported = new Dictionary<string, object>
                    {
                        ["lastMaintenance"] = "2025-12-15",
                        ["uptimeHours"] = 12450
                    }
                }
            }
        };

        // Modern dialysis machine (newer model with built-in TPM)
        var modernDevice = new Device
        {
            DeviceId = "medical-dialysis-modern-001",
            DeviceKey = GenerateDeviceKey(),
            Status = DeviceStatus.Enabled,
            ConnectionState = DeviceConnectionState.Connected,
            TemplateId = "dialysis-modern-v2",
            Tags = new Dictionary<string, string>
            {
                ["manufacturer"] = "MedTech Corp",
                ["model"] = "Dialysis iQ",
                ["type"] = "dialysis",
                ["location"] = "Berlin-Center-01",
                ["firmware"] = "6.0.3",
                ["tpmEnabled"] = "true"  // Newer devices have TPM 2.0
            },
            Twin = new DeviceTwin
            {
                Properties = new TwinProperties
                {
                    Desired = new Dictionary<string, object>
                    {
                        ["pollingInterval"] = 250,  // Faster for newer devices
                        ["maxBloodFlow"] = 500,
                        ["aiEnabled"] = true
                    },
                    Reported = new Dictionary<string, object>
                    {
                        ["lastMaintenance"] = "2025-01-10",
                        ["uptimeHours"] = 3240,
                        ["tpmVersion"] = "2.0"
                    }
                }
            }
        };

        _devices.TryAdd(legacyDevice.DeviceId, legacyDevice);
        _devices.TryAdd(modernDevice.DeviceId, modernDevice);

        _logger.LogInformation("Initialized IoT Hub with {Count} sample devices", _devices.Count);
    }

    // ===== Device Registry (Azure IoT Hub Device Registry) =====

    public IEnumerable<Device> GetAllDevices() => _devices.Values.OrderBy(d => d.DeviceId);

    public Device? GetDevice(string deviceId) => _devices.TryGetValue(deviceId, out var device) ? device : null;

    public Device CreateDevice(string deviceId, string? templateId = null)
    {
        var device = new Device
        {
            DeviceId = deviceId,
            DeviceKey = GenerateDeviceKey(),
            Status = DeviceStatus.Enabled,
            TemplateId = templateId ?? "generic-medical-device",
            ConnectionState = DeviceConnectionState.Disconnected
        };

        _devices.TryAdd(deviceId, device);
        _logger.LogInformation("Device {DeviceId} created with template {Template}", deviceId, templateId);

        return device;
    }

    public bool DeleteDevice(string deviceId)
    {
        _logger.LogInformation("Device {DeviceId} deleted", deviceId);
        return _devices.TryRemove(deviceId, out _);
    }

    public void UpdateDeviceConnection(string deviceId, DeviceConnectionState state)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.ConnectionState = state;
            device.LastActivityTime = DateTime.UtcNow;
            _logger.LogDebug("Device {DeviceId} connection state: {State}", deviceId, state);
        }
    }

    // ===== Device Twins (Azure IoT Hub Device Twins) =====

    public DeviceTwin? GetTwin(string deviceId)
    {
        return _devices.TryGetValue(deviceId, out var device) ? device.Twin : null;
    }

    public DeviceTwin? UpdateTwinDesiredProperties(string deviceId, Dictionary<string, object> desiredProperties)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            foreach (var prop in desiredProperties)
            {
                device.Twin.Properties.Desired[prop.Key] = prop.Value;
            }
            device.Twin.ETag = Guid.NewGuid().ToString();

            _logger.LogInformation("Device {DeviceId} desired properties updated: {@Properties}", deviceId, desiredProperties);
            return device.Twin;
        }
        return null;
    }

    public DeviceTwin? UpdateTwinReportedProperties(string deviceId, Dictionary<string, object> reportedProperties)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            foreach (var prop in reportedProperties)
            {
                device.Twin.Properties.Reported[prop.Key] = prop.Value;
            }
            device.Twin.ETag = Guid.NewGuid().ToString();

            _logger.LogInformation("Device {DeviceId} reported properties updated: {@Properties}", deviceId, reportedProperties);
            return device.Twin;
        }
        return null;
    }

    // ===== Direct Methods (Azure IoT Hub Direct Methods) =====

    public async Task<MethodResponse> InvokeDirectMethodAsync(string deviceId, DirectMethod method)
    {
        if (!_devices.TryGetValue(deviceId, out var device))
        {
            return new MethodResponse
            {
                Status = 404,
                Payload = new { error = "Device not found" },
                CorrelationId = method.CorrelationId
            };
        }

        _logger.LogInformation("Invoking method {Method} on device {DeviceId}", method.MethodName, deviceId);

        // Simulate async device response (real devices would respond via MQTT)
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(50, 200)));

        return method.MethodName switch
        {
            "EmergencyStop" => new MethodResponse
            {
                Status = 200,
                Payload = new { result = "stopped", timestamp = DateTime.UtcNow },
                CorrelationId = method.CorrelationId
            },
            "Reboot" => new MethodResponse
            {
                Status = 200,
                Payload = new { result = "rebooting", estimatedDowntime = "00:02:00" },
                CorrelationId = method.CorrelationId
            },
            "GetDiagnostics" => new MethodResponse
            {
                Status = 200,
                Payload = new
                {
                    uptime = TimeSpan.FromHours(12450),
                    memoryUsage = "245MB",
                    cpuUsage = 12.4,
                    lastError = (string?)null
                },
                CorrelationId = method.CorrelationId
            },
            "FirmwareUpdate" => new MethodResponse
            {
                Status = 202,
                Payload = new { result = "accepted", version = method.Payload?["version"] },
                CorrelationId = method.CorrelationId
            },
            _ => new MethodResponse
            {
                Status = 501,
                Payload = new { error = $"Method '{method.MethodName}' not implemented" },
                CorrelationId = method.CorrelationId
            }
        };
    }

    // ===== Telemetry (Azure IoT Hub Telemetry) =====

    public void IngestTelemetry(TelemetryMessage telemetry)
    {
        _telemetryBuffer.Enqueue(telemetry);

        if (_devices.TryGetValue(telemetry.DeviceId, out var device))
        {
            device.LastActivityTime = DateTime.UtcNow;
            if (device.ConnectionState == DeviceConnectionState.Disconnected)
            {
                device.ConnectionState = DeviceConnectionState.Connected;
            }
        }

        _logger.LogDebug("Telemetry received from {DeviceId}: {@Data}", telemetry.DeviceId, telemetry.Data);
    }

    public IEnumerable<TelemetryMessage> GetRecentTelemetry(string deviceId, int count = 100)
    {
        return _telemetryBuffer
            .Where(t => t.DeviceId == deviceId)
            .OrderByDescending(t => t.EnqueuedTime)
            .Take(count);
    }

    public TelemetryStats GetTelemetryStats()
    {
        return new TelemetryStats
        {
            TotalMessages = _telemetryBuffer.Count,
            ConnectedDevices = _devices.Values.Count(d => d.ConnectionState == DeviceConnectionState.Connected),
            TotalDevices = _devices.Count
        };
    }

    // ===== Device Provisioning Service (DPS) Pattern =====

    public ProvisioningResponse ProvisionDevice(ProvisioningRequest request)
    {
        var deviceId = $"braun-{request.RegistrationId}-{Guid.NewGuid().ToString()[..8]}";

        var device = new Device
        {
            DeviceId = deviceId,
            DeviceKey = GenerateDeviceKey(),
            Status = DeviceStatus.Enabled,
            TemplateId = request.TemplateId ?? "generic-medical",
            ConnectionState = DeviceConnectionState.Disconnected
        };

        // Copy payload to tags
        if (request.Payload != null)
        {
            foreach (var tag in request.Payload)
            {
                device.Tags[tag.Key] = tag.Value;
            }
        }

        _devices.TryAdd(deviceId, device);

        _logger.LogInformation("Device provisioned: {DeviceId} from registration {RegistrationId}",
            deviceId, request.RegistrationId);

        return new ProvisioningResponse
        {
            DeviceId = deviceId,
            AssignedHub = "mededge-iot-hub-simulated",
            DeviceKey = device.DeviceKey,
            AssignedAt = DateTime.UtcNow,
            Status = ProvisioningStatus.Assigned
        };
    }

    // ===== TPM Attestation (Hardware Security Module) =====

    public bool ValidateTpmAttestation(string deviceId, TpmAttestation attestation)
    {
        // Simulate TPM validation
        // In production: Verify EK certificate chain, check against manufacturer CA
        var isValid = !string.IsNullOrEmpty(attestation.EndorsementKey)
                   && !string.IsNullOrEmpty(attestation.StorageRootKey)
                   && (attestation.ValidTo == null || attestation.ValidTo > DateTime.UtcNow);

        if (isValid && _devices.TryGetValue(deviceId, out var device))
        {
            device.Tags["tpmEndorsementKey"] = attestation.EndorsementKey[..20] + "...";
            device.Tags["tpmValidated"] = "true";
        }

        _logger.LogInformation("TPM attestation for {DeviceId}: {Result}", deviceId, isValid ? "Valid" : "Invalid");
        return isValid;
    }

    private string GenerateDeviceKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}

public record TelemetryStats
{
    public int TotalMessages { get; init; }
    public int ConnectedDevices { get; init; }
    public int TotalDevices { get; init; }
}
