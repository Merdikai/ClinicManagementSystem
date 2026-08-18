namespace ClinicManagementSystem.Application.DTOs;

public record CreateDoctorScheduleDto(
    Guid DoctorId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int SlotDurationMinutes
);

public record DoctorScheduleResponseDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int SlotDurationMinutes,
    bool IsActive
);
