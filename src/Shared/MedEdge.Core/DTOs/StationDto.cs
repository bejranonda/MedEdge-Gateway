namespace MedEdge.Core.DTOs;

/// <summary>
/// Data transfer object for treatment stations.
/// </summary>
public record StationDto(
    string Id,
    string StationNumber,
    string Status,
    string AreaId,
    string AreaName,
    string? CurrentTreatmentId,
    string? CurrentPatientId,
    string? PhysicalLocation,
    int DeviceCount,
    StationConfigurationDto? Configuration
);

public record StationSummaryDto(
    string Id,
    string StationNumber,
    string Status,
    string AreaId,
    string AreaName,
    string? CurrentPatientName
);

public record CreateStationRequest(
    string StationNumber,
    string AreaId,
    string? PhysicalLocation,
    int DisplayOrder = 0
);

public record UpdateStationRequest(
    string StationNumber,
    string Status,
    string? PhysicalLocation,
    string? Notes
);

public record UpdateStationStatusRequest(
    string Status,
    string? CurrentTreatmentId,
    string? CurrentPatientId
);

/// <summary>
/// Data transfer object for station configuration.
/// </summary>
public record StationConfigurationDto(
    string Id,
    string StationId,
    bool HasWaterSupply,
    bool HasPowerBackup,
    bool HasOxygenSupply,
    bool HasVacuumSupply,
    int MaxDeviceSlots,
    Dictionary<string, string> DeviceSlots,
    string TreatmentTypes
);
