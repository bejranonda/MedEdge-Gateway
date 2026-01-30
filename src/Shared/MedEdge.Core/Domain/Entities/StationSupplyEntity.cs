namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Inventory levels for supplies at a specific station.
/// </summary>
public class StationSupplyEntity
{
    public string Id { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public string SupplyId { get; set; } = default!;
    public int QuantityOnHand { get; set; } = 0;
    public int MinimumQuantity { get; set; } = 5; // Station-specific minimum
    public DateTime? LastRestocked { get; set; }
    public DateTime? LastConsumed { get; set; }
    public DateTime? NextExpirationCheck { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public StationEntity Station { get; set; } = default!;
    public SupplyEntity Supply { get; set; } = default!;
    public ICollection<SupplyLotEntity> Lots { get; set; } = new List<SupplyLotEntity>();
}
