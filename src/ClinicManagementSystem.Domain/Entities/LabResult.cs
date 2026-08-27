using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Entities;

public class LabResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LabOrderItemId { get; set; }
    public LabOrderItem LabOrderItem { get; set; } = null!;

    public Guid? PerformedByTechnicianId { get; set; }
    public User? PerformedByTechnician { get; set; }
    public Guid? VerifiedByDoctorId { get; set; }
    public User? VerifiedByDoctor { get; set; }

    public DateTime ResultDate { get; set; } = DateTime.UtcNow;
    public string ParameterResultsJson { get; set; } = "[]"; // JSON array of { parameterName, value, unit, minRef, maxRef, flag: Normal/Low/High/Critical }
    public string? Remarks { get; set; }
    public bool HasAbnormalFlag { get; set; } = false;
    public string? AttachmentUrl { get; set; }
}
