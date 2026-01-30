namespace MedEdge.Core.DTOs;

/// <summary>
/// Request DTO for creating a device group.
/// </summary>
public record CreateDeviceGroupRequest(
    string StationId,
    string GroupName,
    string GroupType,
    List<string> DeviceIds,
    Dictionary<string, string> CoordinationRules,
    string? Description,
    int DisplayOrder = 0
);

/// <summary>
/// Request DTO for updating a device group.
/// </summary>
public record UpdateDeviceGroupRequest(
    string GroupName,
    string GroupType,
    List<string> DeviceIds,
    Dictionary<string, string> CoordinationRules,
    string? Description,
    int DisplayOrder
);

/// <summary>
/// Request DTO for executing a station command.
/// </summary>
public record ExecuteStationCommandRequest(
    string StationId,
    string Operation,
    Dictionary<string, object> Parameters,
    List<string> TargetDevices,
    List<string> TargetGroups,
    DateTime? ScheduledExecutionTime,
    string? RequestedBy
);

/// <summary>
/// DTO for device group display.
/// </summary>
public record DeviceGroupDto(
    string Id,
    string StationId,
    string GroupName,
    string GroupType,
    List<string> DeviceIds,
    int DeviceCount,
    string? Description,
    bool IsActive
);

/// <summary>
/// DTO for coordination command display.
/// </summary>
public record CoordinationCommandDto(
    string Id,
    string StationId,
    string Operation,
    Dictionary<string, object> Parameters,
    int TargetDeviceCount,
    DateTime ScheduledExecutionTime,
    DateTime? ActualExecutionTime,
    string Status,
    string? ResultSummary,
    int SuccessfulCount,
    int FailedCount,
    string? RequestedBy
);
