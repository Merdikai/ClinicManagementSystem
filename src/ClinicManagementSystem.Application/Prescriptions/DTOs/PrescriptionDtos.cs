using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Application.Prescriptions.DTOs;

public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid ConsultationId { get; set; }
    public Guid? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string? PatientAllergies { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime IssuedAt { get; set; }
    public DateTime? DispensedAt { get; set; }
    public Guid? DispensedByUserId { get; set; }
    public string? DispensedByPharmacistName { get; set; }
    public string? VerificationNotes { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class PrescriptionItemDto
{
    public Guid Id { get; set; }
    public Guid MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Category { get; set; }
    public int Quantity { get; set; }
    public int DispensedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public bool IsControlledSubstance { get; set; }
    public string Status { get; set; } = "Pending";
    public string? DosageInstructions { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public record DispensePrescriptionRequestDto(
    string? VerificationNotes = null
);

public class PrescriptionLabelDto
{
    public Guid PrescriptionId { get; set; }
    public string RxNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public int Quantity { get; set; }
    public string DosageInstructions { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DispensedDate { get; set; } = string.Empty;
    public string PharmacistName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = "MedFlow Health Clinic";
    public string ClinicPhone { get; set; } = "+1 (555) 019-2834";
    public string CautionNotes { get; set; } = "Keep out of reach of children. Store in a cool, dry place.";
}

