using System;

namespace ClinicManagementSystem.Application.StaffDuty.DTOs;

public class StaffDutyShiftDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ShiftType { get; set; } = string.Empty;
    public DateTime ClockInTimeUtc { get; set; }
    public DateTime? ClockOutTimeUtc { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string? HandoverNotes { get; set; }
}

public class ClockInDutyDto
{
    public string? Department { get; set; }
    public string? ShiftType { get; set; }
    public string? Notes { get; set; }
}

public class ClockOutDutyDto
{
    public string? HandoverNotes { get; set; }
}
