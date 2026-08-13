namespace ClinicManagementSystem.Application.DTOs;

public record CreateConsultationDto(
    Guid AppointmentId,
    string Symptoms,
    string Diagnosis,
    string ClinicalNotes
);

public class ConsultationResponseDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public DateTime ConsultedAt { get; set; }
    public PrescriptionResponseDto? Prescription { get; set; }
}

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
