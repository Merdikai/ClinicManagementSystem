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
    public string? TriageUrgency { get; set; }
    public string? RoomAssignment { get; set; }
    public string? TriageNotes { get; set; }
    public string? TokenNumber { get; set; }
    public string? VisitType { get; set; }
    public VitalSignResponseDto? VitalSigns { get; set; }
    public List<LinkDto>? Links { get; set; }
}

public record CheckInAppointmentDto(
    string? TokenNumber = null,
    string? VisitType = null
);

public record RescheduleAppointmentDto(
    DateTime NewScheduledDateTime,
    string? Reason = null
);

public record MarkNoShowDto(
    string? Notes = null
);

public record UpdateAppointmentTriageDto(
    string? TriageUrgency,
    string? RoomAssignment,
    string? TriageNotes,
    string? Status = null
);

public record UpdateAppointmentStatusDto(
    string Status,
    string? RoomAssignment = null
);

