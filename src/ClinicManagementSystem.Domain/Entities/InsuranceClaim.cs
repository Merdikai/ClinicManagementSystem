using System;

namespace ClinicManagementSystem.Domain.Entities;

public class InsuranceClaim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string InsuranceProvider { get; set; } = string.Empty; // e.g., BlueCross BlueShield, Aetna, Cigna, UnitedHealthcare
    public string PolicyNumber { get; set; } = string.Empty;
    public string? GroupNumber { get; set; }
    public decimal ClaimAmount { get; set; }
    public decimal CoveredAmount { get; set; }
    public decimal PatientCopay { get; set; }
    public string Status { get; set; } = "Submitted"; // Submitted, UnderReview, Approved, PartiallyPaid, Rejected, Settled
    public string? RemittanceReference { get; set; }
    public string? DenialReason { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }
    public string? Notes { get; set; }
}
