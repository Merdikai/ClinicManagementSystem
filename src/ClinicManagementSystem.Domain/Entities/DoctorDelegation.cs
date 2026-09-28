using System;

namespace ClinicManagementSystem.Domain.Entities;

public class DoctorDelegation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OriginalDoctorId { get; set; }
    public User? OriginalDoctor { get; set; }
    public string DelegatingDoctorName { get; set; } = string.Empty;
    public Guid CoveringDoctorId { get; set; }
    public User? CoveringDoctor { get; set; }
    public string CoveringDoctorName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
