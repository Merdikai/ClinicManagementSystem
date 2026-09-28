using System;

namespace ClinicManagementSystem.Domain.Entities;

public class NursingProcedure
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? OrderedByDoctorId { get; set; }
    public User? OrderedByDoctor { get; set; }

    public Guid? PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }

    public string ProcedureType { get; set; } = string.Empty; // Injections, IV Fluids, Wound Dressing, Nebulization, Catheterization, Vitals Re-check
    public string BodySite { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string PatientTolerance { get; set; } = "Well Tolerated";
    public string SuppliesUsed { get; set; } = string.Empty;

    public string Status { get; set; } = "Completed"; // "Ordered", "InProgress", "Completed", "Cancelled"
    public string Priority { get; set; } = "Routine"; // "Routine", "Urgent", "Stat"
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PerformedAt { get; set; } = DateTime.UtcNow;

    public decimal Price { get; set; } = 0m;
    public bool IsBilled { get; set; } = false;
}
