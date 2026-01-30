namespace MedEdge.Dashboard.Models;

/// <summary>
/// Data transfer object for treatment areas/zones for dashboard consumption.
/// </summary>
public record TreatmentAreaDto(
    string Id,
    string Name,
    string AreaType,
    int Capacity,
    string? ParentAreaId,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    int OccupiedStations,
    int AvailableStations
);

/// <summary>
/// Data transfer object for stations for dashboard consumption.
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

/// <summary>
/// Summary DTO for stations in dashboard lists.
/// </summary>
public record StationSummaryDto(
    string Id,
    string StationNumber,
    string Status,
    string AreaId,
    string AreaName,
    string? CurrentPatientName
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

/// <summary>
/// Request DTO for creating a treatment area.
/// </summary>
public record CreateTreatmentAreaRequest(
    string Name,
    string AreaType,
    int Capacity,
    string? ParentAreaId,
    string? Description,
    int DisplayOrder = 0
);

/// <summary>
/// Request DTO for updating a treatment area.
/// </summary>
public record UpdateTreatmentAreaRequest(
    string Name,
    string AreaType,
    int Capacity,
    string? Description,
    int DisplayOrder,
    bool IsActive
);

/// <summary>
/// Request DTO for creating a station.
/// </summary>
public record CreateStationRequest(
    string StationNumber,
    string AreaId,
    string? PhysicalLocation,
    int DisplayOrder = 0
);

/// <summary>
/// Request DTO for updating a station.
/// </summary>
public record UpdateStationRequest(
    string StationNumber,
    string Status,
    string? PhysicalLocation,
    string? Notes
);

/// <summary>
/// Request DTO for updating a station's status.
/// </summary>
public record UpdateStationStatusRequest(
    string Status,
    string? CurrentTreatmentId,
    string? CurrentPatientId
);
