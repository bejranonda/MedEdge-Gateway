namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Represents a group of devices that can work together at a station.
/// </summary>
public class DeviceGroupEntity
{
    public string Id { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public string GroupName { get; set; } = default!; // e.g., "Primary", "Secondary", "Monitoring"
    public string GroupType { get; set; } = "treatment"; // treatment, monitoring, support
    public List<string> DeviceIds { get; set; } = new();
    public Dictionary<string, string> CoordinationRules { get; set; } = new();
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public StationEntity Station { get; set; } = default!;
    public ICollection<DeviceCoordinationCommand> CoordinationCommands { get; set; } = new List<DeviceCoordinationCommand>();
}

/// <summary>
/// Represents a coordinated command to be executed across multiple devices.
/// </summary>
public class DeviceCoordinationCommand
{
    public string Id { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public string Operation { get; set; } = default!; // start-all, stop-all, sync-parameters, emergency-stop
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> TargetDevices { get; set; } = new();
    public List<string> TargetGroups { get; set; } = new();
    public DateTime ScheduledExecutionTime { get; set; }
    public DateTime? ActualExecutionTime { get; set; }
    public string Status { get; set; } = "pending"; // pending, executing, completed, failed, cancelled
    public string? ResultSummary { get; set; }
    public Dictionary<string, string> DeviceResults { get; set; } = new(); // deviceId -> result status
    public string? ErrorMessage { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public StationEntity Station { get; set; } = default!;
}
