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
