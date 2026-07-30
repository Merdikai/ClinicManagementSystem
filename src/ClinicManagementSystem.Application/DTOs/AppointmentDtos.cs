namespace ClinicManagementSystem.Application.DTOs;

public record CreateAppointmentDto(
    Guid PatientId,
    Guid DoctorId,
    DateTime ScheduledDateTime,
    int DurationMinutes,
    string ReasonForVisit
);

public record AppointmentResponseDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateTime ScheduledDateTime,
    int DurationMinutes,
    string Status,
    string ReasonForVisit
);