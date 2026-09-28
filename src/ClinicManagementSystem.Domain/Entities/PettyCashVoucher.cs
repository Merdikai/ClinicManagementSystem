using System;

namespace ClinicManagementSystem.Domain.Entities;

public class PettyCashVoucher
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VoucherNumber { get; set; } = string.Empty;
    public string Type { get; set; } = "Disbursement"; // Disbursement, Replenishment
    public decimal Amount { get; set; }
    public string Category { get; set; } = "ClinicConsumables"; // ClinicConsumables, CourierPostage, Refreshments, UrgentRepairs, LocalTransport, Other
    public string Payee { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid DisbursedByUserId { get; set; }
    public string DisbursedByUserName { get; set; } = string.Empty;
    public decimal RemainingBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ReceiptRef { get; set; }
}
