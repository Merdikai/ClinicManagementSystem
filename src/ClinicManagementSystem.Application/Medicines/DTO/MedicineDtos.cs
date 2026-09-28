namespace ClinicManagementSystem.Application.DTOs;

public record CreateMedicineDto(
    string Code,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice,
    DateTime? ExpiryDate = null,
    string BatchNumber = "",
    string? GenericName = null,
    string? DosageForm = null,
    string? Strength = null,
    string? Manufacturer = null,
    int ReorderLevel = 15,
    decimal CostPrice = 0.0m,
    string? StorageConditions = null,
    bool IsControlledSubstance = false,
    bool IsActive = true
);

public class MedicineResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? DosageForm { get; set; }
    public string? Strength { get; set; }
    public string? Manufacturer { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; } = 15;
    public decimal CostPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string? StorageConditions { get; set; }
    public bool IsControlledSubstance { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLowStock => StockQuantity <= ReorderLevel;
    public bool IsNearExpiry => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow.AddDays(60);
    public List<LinkDto>? Links { get; set; }
}


public record BulkRestockItemDto(
    Guid MedicineId,
    int Quantity
);

public record BulkUpdatePriceItemDto(
    Guid MedicineId,
    decimal NewUnitPrice
);
