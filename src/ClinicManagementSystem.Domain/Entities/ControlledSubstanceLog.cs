using System;

namespace ClinicManagementSystem.Domain.Entities;

public class ControlledSubstanceLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LogNumber { get; set; } = string.Empty;
    
    public Guid MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;
    
    public Guid? PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    
    public int QuantityDispensed { get; set; }
    public int RemainingBalance { get; set; }
    public string PrescribingDoctorName { get; set; } = string.Empty;
    
    public Guid DispensingPharmacistUserId { get; set; }
    public User DispensingPharmacist { get; set; } = null!;
    
    public string? WitnessPharmacistName { get; set; }
    public DateTime DispensedAt { get; set; } = DateTime.UtcNow;
    public string MedicalJustification { get; set; } = string.Empty;
}
