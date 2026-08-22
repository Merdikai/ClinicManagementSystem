namespace ClinicManagementSystem.Application.DTOs;

public class CreateConsultationDto
{
    public Guid AppointmentId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public string? PrescriptionNotes { get; set; }
    public List<CreatePrescriptionItemDto>? PrescriptionItems { get; set; }

    public CreateConsultationDto() { }

    public CreateConsultationDto(Guid appointmentId, string symptoms, string diagnosis, string clinicalNotes)
    {
        AppointmentId = appointmentId;
        Symptoms = symptoms;
        Diagnosis = diagnosis;
        ClinicalNotes = clinicalNotes;
    }
}

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
