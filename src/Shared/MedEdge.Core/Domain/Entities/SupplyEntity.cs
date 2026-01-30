namespace MedEdge.Core.Domain.Entities;

/// <summary>
/// Master catalog of medical supplies and consumables.
/// </summary>
public class SupplyEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Category { get; set; } = "consumable"; // consumable, medication, fluid, equipment
    public string? Sku { get; set; } // Product identifier
    public string? Description { get; set; }
    public string UnitOfMeasure { get; set; } = "each"; // each, ml, l, g, kg
    public int ReorderLevel { get; set; } = 10; // Alert threshold
    public int ReorderQuantity { get; set; } = 50; // Suggested reorder amount
    public int? ShelfLifeDays { get; set; } // For expiration tracking
    public string? StorageLocation { get; set; } // Default storage location
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<StationSupplyEntity> StationSupplies { get; set; } = new List<StationSupplyEntity>();
}
