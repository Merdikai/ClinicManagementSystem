namespace ClinicManagementSystem.Application.DTOs;

public record CreateMedicineDto(
    string Code,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice
);

public class MedicineResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public record DispenseMedicineDto(
    Guid PrescriptionItemId,
    int Quantity
);
