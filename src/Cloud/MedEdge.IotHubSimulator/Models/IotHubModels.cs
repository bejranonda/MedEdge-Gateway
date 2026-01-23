namespace MedEdge.IotHubSimulator.Models;

/// <summary>
/// Simulated Azure IoT Hub Device - represents a medical device in the B. Braun ecosystem
/// </summary>
public class Device
{
    public string DeviceId { get; set; } = string.Empty;
    public string? DeviceKey { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Enabled;
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    public DeviceTwin Twin { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public DeviceConnectionState ConnectionState { get; set; } = DeviceConnectionState.Disconnected;
    public string? TemplateId { get; set; }  // For device templates (e.g., "dialog-plus-v2")
}

public enum DeviceStatus
{
    Enabled,
    Disabled,
    Provisioning
}

public enum DeviceConnectionState
{
    Connected,
    Disconnected,
    Offline
}

/// <summary>
/// Device Twin - mirrors Azure IoT Hub Twin for configuration synchronization
/// </summary>
public class DeviceTwin
{
    public string? ETag { get; set; } = Guid.NewGuid().ToString();
    public TwinProperties Properties { get; set; } = new();
    public Dictionary<string, object> Desired { get; set; } = new();
    public Dictionary<string, object> Reported { get; set; } = new();
}

public class TwinProperties
{
    public Dictionary<string, object> Desired { get; set; } = new();
    public Dictionary<string, object> Reported { get; set; } = new();
}

/// <summary>
/// Device Provisioning Service request
/// </summary>
public class ProvisioningRequest
{
    public string RegistrationId { get; set; } = string.Empty;
    public string? DeviceKey { get; set; }
    public string? TemplateId { get; set; }
    public Dictionary<string, string>? Payload { get; set; }
}

public class ProvisioningResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public string? AssignedHub { get; set; }
    public string? DeviceKey { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public ProvisioningStatus Status { get; set; }
}

public enum ProvisioningStatus
{
    Assigned,
    Failed,
    Unassigned
}

/// <summary>
/// Direct Method invocation (Azure IoT Hub Direct Methods pattern)
/// </summary>
public class DirectMethod
{
    public string MethodName { get; set; } = string.Empty;
    public Dictionary<string, object>? Payload { get; set; }
    public string? CorrelationId { get; set; }
    public int TimeoutInSeconds { get; set; } = 30;
}

public class MethodResponse
{
    public int Status { get; set; }  // 200 = success, 404 = method not found, 504 = timeout
    public object? Payload { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Telemetry message from device
/// </summary>
public class TelemetryMessage
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime EnqueuedTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
    public string? CorrelationId { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
}

/// <summary>
/// Represents a TPM (Trusted Platform Module) attestation record
/// For B. Braun: Hardware security module for device identity
/// </summary>
public class TpmAttestation
{
    public string EndorsementKey { get; set; } = string.Empty;
    public string StorageRootKey { get; set; } = string.Empty;
    public string? AttestationCertificate { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }
}
