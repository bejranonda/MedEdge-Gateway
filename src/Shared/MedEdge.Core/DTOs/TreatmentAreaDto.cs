namespace MedEdge.Core.DTOs;

/// <summary>
/// Data transfer object for treatment areas/zones.
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

public record CreateTreatmentAreaRequest(
    string Name,
    string AreaType,
    int Capacity,
    string? ParentAreaId,
    string? Description,
    int DisplayOrder = 0
);

public record UpdateTreatmentAreaRequest(
    string Name,
    string AreaType,
    int Capacity,
    string? Description,
    int DisplayOrder,
    bool IsActive
);
