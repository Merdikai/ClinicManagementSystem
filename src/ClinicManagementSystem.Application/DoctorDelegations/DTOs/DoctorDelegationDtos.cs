using System;

namespace ClinicManagementSystem.Application.DoctorDelegations.DTOs;

public class DoctorDelegationDto
{
    public Guid Id { get; set; }
    public Guid OriginalDoctorId { get; set; }
    public string DelegatingDoctorName { get; set; } = string.Empty;
    public Guid CoveringDoctorId { get; set; }
    public string CoveringDoctorName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateDoctorDelegationDto
{
    public Guid? OriginalDoctorId { get; set; }
    public string? DelegatingDoctorName { get; set; }
    public Guid? CoveringDoctorId { get; set; }
    public string CoveringDoctorName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
