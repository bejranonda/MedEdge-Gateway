namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Represents a treatment station where patients receive treatment.
/// A station contains multiple medical devices and is assigned to a single patient at a time.
/// </summary>
public class StationEntity
{
    public string Id { get; set; } = default!;
    public string StationNumber { get; set; } = default!; // e.g., "A-01", "B-05"
    public string Status { get; set; } = "available"; // available, occupied, maintenance, cleaning, offline
    public string AreaId { get; set; } = default!;
    public string? CurrentTreatmentId { get; set; }
    public string? CurrentPatientId { get; set; }
    public int DisplayOrder { get; set; } = 0; // For UI ordering within area
    public string? PhysicalLocation { get; set; } // e.g., "Room 101, Bed 1"
    public DateTime? LastMaintenanceDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public TreatmentAreaEntity Area { get; set; } = default!;
    public StationConfigurationEntity Configuration { get; set; } = default!;
    public ICollection<FhirDeviceEntity> Devices { get; set; } = new List<FhirDeviceEntity>();
    public ICollection<StationSupplyEntity> Supplies { get; set; } = new List<StationSupplyEntity>();
}
