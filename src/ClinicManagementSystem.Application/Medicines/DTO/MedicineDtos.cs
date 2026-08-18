namespace ClinicManagementSystem.Application.DTOs;

public record CreateMedicineDto(
    string Code,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice,
    DateTime? ExpiryDate = null,
    string BatchNumber = ""
);

public class MedicineResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
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
