namespace ClinicManagementSystem.Domain.Entities;

public class EmrAccessAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string AccessType { get; set; } = "ViewProfile"; // ViewProfile, ViewVitals, ViewConsultations, ViewLabResults, BreakGlassOverride
    public string? Reason { get; set; }
    public bool IsBreakGlassEmergency { get; set; } = false;
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
