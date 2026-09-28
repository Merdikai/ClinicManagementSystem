using System;

namespace ClinicManagementSystem.Domain.Entities;

public class LabReagent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string CatalogCode { get; set; } = string.Empty;
    public string Category { get; set; } = "Hematology"; // Hematology, Clinical Chemistry, Immunoassay, Urinalysis, Consumables & Phlebotomy, Microbiology
    public string LotNumber { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumThreshold { get; set; } = 10;
    public string UnitOfMeasure { get; set; } = "Tests"; // Tests, Kits, mL, Boxes, Packs, Tubes
    public DateTime ExpiryDate { get; set; }
    public string StorageCondition { get; set; } = "2-8°C Refrigerated"; // 2-8°C Refrigerated, 15-25°C Room Temp, -20°C Frozen, Dark & Dry
    public string Status { get; set; } = "InStock"; // InStock, LowStock, Expired
    public string? Manufacturer { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? LastRestockedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
