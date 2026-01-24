namespace MedEdge.Dashboard.Models;

/// <summary>
/// Overall system health status
/// </summary>
public class SystemHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public List<ComponentHealth> Components { get; set; } = new();
    public int HealthScore { get; set; } // 0-100
}

/// <summary>
/// Individual component health status
/// </summary>
public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Gateway, Service, Database, Broker
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public string? Message { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public double ResponseTimeMs { get; set; }
    public Dictionary<string, string> Metrics { get; set; } = new();
}

/// <summary>
/// Edge device status with detailed information
/// </summary>
public class EdgeDeviceStatus
{
    public string DeviceId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string? CurrentPatientId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastTelemetryTime { get; set; }
    public int ActiveAlarmCount { get; set; }
    public string RiskLevel { get; set; } = "Low"; // Low, Moderate, High, Critical
    public DeviceMetrics? Metrics { get; set; }
}

/// <summary>
/// Real-time device metrics
/// </summary>
public class DeviceMetrics
{
    public double BloodFlow { get; set; }
    public double ArterialPressure { get; set; }
    public double VenousPressure { get; set; }
    public double Temperature { get; set; }
    public double Conductivity { get; set; }
    public int TreatmentTime { get; set; }
}

/// <summary>
/// Alert information
/// </summary>
public class AlertInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DeviceId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Info, Warning, Error, Critical
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
}

/// <summary>
/// System statistics for dashboard
/// </summary>
public class SystemStatistics
{
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int TotalObservations { get; set; }
    public int ActiveAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public double TelemetryRate { get; set; } // messages per second
    public TimeSpan SystemUptime { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System flow information for visualization
/// </summary>
public class SystemFlowInfo
{
    public List<FlowNode> Nodes { get; set; } = new();
    public List<FlowConnection> Connections { get; set; } = new();
    public List<FlowData> ActiveDataFlows { get; set; } = new();
}

/// <summary>
/// Flow node in the system diagram
/// </summary>
public class FlowNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Device, Gateway, Service, Broker, Database
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = "Unknown";
    public int DataThroughput { get; set; } // messages per minute
}

/// <summary>
/// Connection between flow nodes
/// </summary>
public class FlowConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty; // Modbus, MQTT, HTTP, SignalR
    public bool IsActive { get; set; }
    public int DataRate { get; set; } // messages per minute
}

/// <summary>
/// Active data flow for animation
/// </summary>
public class FlowData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConnectionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Telemetry, Command, Alert
    public double Progress { get; set; } // 0-1 for animation position
    public string? SourceDeviceId { get; set; }
}
