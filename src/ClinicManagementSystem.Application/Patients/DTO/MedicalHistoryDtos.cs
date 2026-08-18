namespace ClinicManagementSystem.Application.DTOs;

public record MedicalHistoryResponseDto(
    Guid PatientId,
    string PatientName,
    IEnumerable<VisitRecordDto> Visits
);

public record VisitRecordDto(
    Guid AppointmentId,
    DateTime VisitDate,
    string DoctorName,
    string ReasonForVisit,
    string Diagnosis,
    string Symptoms,
    VitalSignResponseDto? VitalSigns,
    PrescriptionResponseDto? Prescription
);
