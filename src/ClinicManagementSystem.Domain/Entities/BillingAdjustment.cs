using System;

namespace ClinicManagementSystem.Domain.Entities;

public class BillingAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AdjustmentNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public string Type { get; set; } = "Refund"; // Refund, Discount, WriteOff, DisputeCorrection
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
}
