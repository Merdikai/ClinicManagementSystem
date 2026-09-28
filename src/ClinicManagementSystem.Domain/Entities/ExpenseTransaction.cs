using System;

namespace ClinicManagementSystem.Domain.Entities;

public class ExpenseTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExpenseNumber { get; set; } = string.Empty;
    public string Category { get; set; } = "PharmaceuticalSupplies"; // PharmaceuticalSupplies, MedicalEquipment, FacilityUtilities, Maintenance, SoftwareLicense, OfficeSupplies, Other
    public string VendorName { get; set; } = string.Empty;
    public string InvoiceReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? PaidDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer"; // BankTransfer, CorporateCard, EFT, Cheque, Cash
    public string Status { get; set; } = "PendingApproval"; // PendingApproval, Approved, Paid, Cancelled
    public Guid? PharmacyStockRequisitionId { get; set; }
    public PharmacyStockRequisition? PharmacyStockRequisition { get; set; }
    public Guid RecordedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
