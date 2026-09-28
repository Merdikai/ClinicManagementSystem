using System;

namespace ClinicManagementSystem.Application.EmrAccess.DTOs;

public class LogEmrAccessDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string AccessType { get; set; } = "ViewChart";
    public string? ActionDetail { get; set; }
}

public class BreakGlassRequestDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string? ClinicalContext { get; set; }
}

public class EmrAccessAuditLogDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string AccessedByUserId { get; set; } = string.Empty;
    public string AccessedByUserName { get; set; } = string.Empty;
    public string AccessedByRole { get; set; } = string.Empty;
    public string AccessType { get; set; } = string.Empty;
    public string? ActionDetail { get; set; }
    public bool IsEmergencyOverride { get; set; }
    public string? BreakGlassRationale { get; set; }
    public DateTime AccessedAtUtc { get; set; }
}
