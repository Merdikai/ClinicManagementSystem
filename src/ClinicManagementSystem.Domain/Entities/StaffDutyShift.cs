using System;

namespace ClinicManagementSystem.Domain.Entities;

public class StaffDutyShift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = "General Practice";
    public string ShiftType { get; set; } = "Day Shift";
    public DateTime ClockInTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClockOutTimeUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string? HandoverNotes { get; set; }
}
