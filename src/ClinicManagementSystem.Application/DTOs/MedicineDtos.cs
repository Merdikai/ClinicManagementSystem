namespace ClinicManagementSystem.Application.DTOs;

public record CreateMedicineDto(
    string Code,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice
);

public record MedicineResponseDto(
    Guid Id,
    string Code,
    string Name,
    string Category,
    int StockQuantity,
    decimal UnitPrice
);

public record DispenseMedicineDto(
    Guid PrescriptionItemId,
    int Quantity
);