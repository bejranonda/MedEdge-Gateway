namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Represents a treatment area or zone within a treatment center.
/// Examples: "Zone A", "Ward 3", "ICU", "General Ward"
/// </summary>
public class TreatmentAreaEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string AreaType { get; set; } = "general"; // dialysis, icu, general
    public int Capacity { get; set; } = 10; // Number of stations
    public string? ParentAreaId { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0; // For UI ordering
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public TreatmentAreaEntity? ParentArea { get; set; }
    public ICollection<TreatmentAreaEntity> SubAreas { get; set; } = new List<TreatmentAreaEntity>();
    public ICollection<StationEntity> Stations { get; set; } = new List<StationEntity>();
}
