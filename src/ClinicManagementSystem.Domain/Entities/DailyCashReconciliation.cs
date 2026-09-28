using System;

namespace ClinicManagementSystem.Domain.Entities;

public class DailyCashReconciliation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReceptionistUserId { get; set; }
    public User ReceptionistUser { get; set; } = null!;
    public DateTime ReconciliationDate { get; set; } = DateTime.UtcNow;
    public string Shift { get; set; } = "Morning"; // "Morning", "Afternoon", "Night", "Full Day"
    public decimal CashCollected { get; set; }
    public decimal CardCollected { get; set; }
    public decimal DigitalCollected { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal ClosingCashCounted { get; set; }
    public decimal DiscrepancyAmount { get; set; }
    public int InvoicesSettledCount { get; set; }
    public string Status { get; set; } = "Balanced"; // "Balanced", "Discrepancy", "Closed"
    public string? Notes { get; set; }
    public Guid? AuditedByUserId { get; set; }
    public string? AuditedByUserName { get; set; }
    public DateTime? AuditedAt { get; set; }
    public string AuditStatus { get; set; } = "PendingAudit"; // "PendingAudit", "VerifiedBalanced", "DiscrepancyApproved", "Disputed"
    public string? AuditNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
