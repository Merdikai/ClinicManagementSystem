using System;

namespace ClinicManagementSystem.Application.DTOs;

public class DailyCashSummaryDto
{
    public DateTime ReconciliationDate { get; set; } = DateTime.UtcNow;
    public decimal CashCollected { get; set; }
    public decimal CardCollected { get; set; }
    public decimal DigitalCollected { get; set; }
    public decimal TotalCollected { get; set; }
    public int InvoicesSettledCount { get; set; }
    public string ReceptionistName { get; set; } = string.Empty;
}

public class CreateDailyCashReconciliationDto
{
    public string Shift { get; set; } = "Morning";
    public decimal CashCollected { get; set; }
    public decimal CardCollected { get; set; }
    public decimal DigitalCollected { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal ClosingCashCounted { get; set; }
    public string? Notes { get; set; }
}

public class DailyCashReconciliationResponseDto
{
    public Guid Id { get; set; }
    public Guid ReceptionistUserId { get; set; }
    public string ReceptionistName { get; set; } = string.Empty;
    public DateTime ReconciliationDate { get; set; }
    public string Shift { get; set; } = "Morning";
    public decimal CashCollected { get; set; }
    public decimal CardCollected { get; set; }
    public decimal DigitalCollected { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal ClosingCashCounted { get; set; }
    public decimal DiscrepancyAmount { get; set; }
    public int InvoicesSettledCount { get; set; }
    public string Status { get; set; } = "Balanced";
    public string? Notes { get; set; }
    public Guid? AuditedByUserId { get; set; }
    public string? AuditedByUserName { get; set; }
    public DateTime? AuditedAt { get; set; }
    public string? AuditStatus { get; set; }
    public string? AuditNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}
