namespace ClinicManagementSystem.Application.DTOs;

public record CreatePrescriptionDto(
    Guid ConsultationId,
    string Notes,
    List<CreatePrescriptionItemDto> Items
);

public record CreatePrescriptionItemDto(
    Guid MedicineId,
    int Quantity,
    string DosageInstructions
);

public class PrescriptionResponseDto
{
    public Guid Id { get; set; }
    public DateTime IssuedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<PrescriptionItemResponseDto> Items { get; set; } = new();
}

public class PrescriptionItemResponseDto
{
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string DosageInstructions { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public record DispensePrescriptionDto(Guid PrescriptionId);
public record DispensedItemDto(
    string MedicineName,
    int RequestedQuantity,
    int DispensedQuantity,
    decimal UnitPrice,
    decimal TotalPrice,
    bool InStock
);
public record DispenseResponseDto(
    Guid PrescriptionId,
    bool FullyDispensed,
    List<DispensedItemDto> Items,
    decimal TotalCost
);
