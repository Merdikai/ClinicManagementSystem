using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Domain.Entities;

public class Prescription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";
    public DateTime? DispensedAt { get; set; }
    public Guid? DispensedByUserId { get; set; }
    public User? DispensedByUser { get; set; }
    public string? VerificationNotes { get; set; }

    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}