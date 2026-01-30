namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Represents a specific lot/batch of supplies with expiration tracking.
/// </summary>
public class SupplyLotEntity
{
    public string Id { get; set; } = default!;
    public string StationSupplyId { get; set; } = default!;
    public string LotNumber { get; set; } = default!;
    public int Quantity { get; set; } = 0;
    public DateTime ExpirationDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? Supplier { get; set; }
    public string? Notes { get; set; }
    public bool IsExpired { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public StationSupplyEntity StationSupply { get; set; } = default!;
}
