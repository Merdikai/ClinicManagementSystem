namespace ClinicManagementSystem.Application.DTOs;

public record CreateConsultationDto(
    Guid AppointmentId,
    string Symptoms,
    string Diagnosis,
    string ClinicalNotes
);

public record ConsultationResponseDto(
    Guid Id,
    Guid AppointmentId,
    string DoctorName,
    string Symptoms,
    string Diagnosis,
    string ClinicalNotes,
    DateTime ConsultedAt,
    PrescriptionResponseDto? Prescription
);

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

public record PrescriptionResponseDto(
    Guid Id,
    DateTime IssuedAt,
    string Notes,
    List<PrescriptionItemResponseDto> Items
);

public record PrescriptionItemResponseDto(
    string MedicineName,
    int Quantity,
    string DosageInstructions,
    decimal UnitPrice,
    decimal TotalPrice
);