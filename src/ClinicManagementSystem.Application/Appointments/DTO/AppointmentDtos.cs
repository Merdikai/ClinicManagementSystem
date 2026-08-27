namespace ClinicManagementSystem.Application.DTOs;

public record CreateAppointmentDto(
    Guid PatientId,
    Guid DoctorId,
    DateTime ScheduledDateTime,
    int DurationMinutes,
    string ReasonForVisit
);

public class AppointmentResponseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReasonForVisit { get; set; } = string.Empty;
    public VitalSignResponseDto? VitalSigns { get; set; }
    public List<LinkDto>? Links { get; set; }
}
