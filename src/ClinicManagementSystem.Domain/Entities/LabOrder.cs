using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Entities;

public class LabOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;
    public LabOrderPriority Priority { get; set; } = LabOrderPriority.Routine;
    public string? ClinicalNotes { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsBilled { get; set; } = false;
    public DateTime? SampleCollectedAt { get; set; }
    public string? CollectedByTechnicianName { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Specimen Accessioning & Quality Validation
    public string? SpecimenBarcode { get; set; }
    public string? SpecimenType { get; set; }
    public string? SampleCondition { get; set; } // Viable, Hemolyzed, Lipemic, Clotted, InsufficientVolume
    public bool IsRejected { get; set; } = false;
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedByTechnicianName { get; set; }
    public bool RequiresRecollection { get; set; } = false;
    public string? TechnicianNotes { get; set; }

    public ICollection<LabOrderItem> Items { get; set; } = new List<LabOrderItem>();
}
