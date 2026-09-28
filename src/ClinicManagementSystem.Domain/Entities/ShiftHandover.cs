using System;

namespace ClinicManagementSystem.Domain.Entities;

public class ShiftHandover
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NurseUserId { get; set; }
    public User NurseUser { get; set; } = null!;

    public string ShiftType { get; set; } = "Morning Shift";
    public string Situation { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;

    public int ActivePatientCount { get; set; } = 0;
    public int CriticalAlertsCount { get; set; } = 0;

    public DateTime HandoverTimestamp { get; set; } = DateTime.UtcNow;
}
