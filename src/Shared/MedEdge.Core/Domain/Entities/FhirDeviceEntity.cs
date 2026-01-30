namespace MedEdge.Core.Domain.Entities;

public class FhirDeviceEntity
{
    public string Id { get; set; } = default!;
    public string? DeviceId { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Status { get; set; } = "active";
    public string? AssignedPatientId { get; set; }
    public string? StationId { get; set; } // Station assignment
    public string? DeviceSlot { get; set; } // Slot position within station (e.g., "slot1", "slot2")
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public FhirPatientEntity? AssignedPatient { get; set; }
    public StationEntity? Station { get; set; }
    public ICollection<FhirObservationEntity> Observations { get; set; } = new List<FhirObservationEntity>();
}
