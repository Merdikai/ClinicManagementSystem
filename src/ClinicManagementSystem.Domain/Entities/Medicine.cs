namespace ClinicManagementSystem.Domain.Entities;

public class Medicine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DosageForm { get; set; } = "Tablet";
    public string Strength { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; } = 15;
    public decimal CostPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string StorageConditions { get; set; } = "Room temperature (15-25°C)";
    public bool IsControlledSubstance { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
