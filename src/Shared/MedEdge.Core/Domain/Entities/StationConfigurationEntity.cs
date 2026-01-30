namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Configuration settings for a treatment station.
/// Defines capabilities, equipment slots, and infrastructure availability.
/// </summary>
public class StationConfigurationEntity
{
    public string Id { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public bool HasWaterSupply { get; set; } = false;
    public bool HasPowerBackup { get; set; } = false;
    public bool HasOxygenSupply { get; set; } = false;
    public bool HasVacuumSupply { get; set; } = false;
    public int MaxDeviceSlots { get; set; } = 5;
    public Dictionary<string, string> DeviceSlots { get; set; } = new(); // Slot assignments: "slot1": "dialysis-machine"
    public string TreatmentTypes { get; set; } = "general"; // comma-separated: hemodialysis,peritoneal,infusion
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public StationEntity Station { get; set; } = default!;
}
