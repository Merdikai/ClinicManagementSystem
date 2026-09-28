using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Application.Finance.DTOs;

public class FinanceDashboardMetricsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal PendingReceivables { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int PendingClaimsCount { get; set; }
    public decimal PendingClaimsAmount { get; set; }
    public int PendingAdjustmentsCount { get; set; }
    public decimal PendingPayablesAmount { get; set; }
    public decimal PettyCashCurrentBalance { get; set; }
    public int UnreconciledDrawersCount { get; set; }
    public List<DepartmentRevenueDto> DepartmentRevenue { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyRevenueTrend { get; set; } = new();
    public List<RecentFinancialActivityDto> RecentActivities { get; set; } = new();
}

public class DepartmentRevenueDto
{
    public string Department { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int InvoicesCount { get; set; }
    public double Percentage { get; set; }
}

public class MonthlyTrendDto
{
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class RecentFinancialActivityDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // "InvoicePayment", "Expense", "InsuranceRemittance", "PettyCash", "Payroll"
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class InsuranceClaimDto
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string InsuranceProvider { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string? GroupNumber { get; set; }
    public decimal ClaimAmount { get; set; }
    public decimal CoveredAmount { get; set; }
    public decimal PatientCopay { get; set; }
    public string Status { get; set; } = "Submitted";
    public string? RemittanceReference { get; set; }
    public string? DenialReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateInsuranceClaimDto
{
    public Guid InvoiceId { get; set; }
    public string InsuranceProvider { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string? GroupNumber { get; set; }
    public decimal ClaimAmount { get; set; }
    public decimal PatientCopay { get; set; }
    public string? Notes { get; set; }
}

public class ReconcileInsuranceClaimDto
{
    public string Status { get; set; } = "Remitted"; // "Approved", "Remitted", "Denied", "Appealed"
    public decimal CoveredAmount { get; set; }
    public string? RemittanceReference { get; set; }
    public string? DenialReason { get; set; }
    public string? Notes { get; set; }
}

public class BillingAdjustmentDto
{
    public Guid Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string Type { get; set; } = "Discount"; // "Discount", "Refund", "BadDebtWriteOff", "CourtesyWaiver"
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Status { get; set; } = "PendingApproval"; // "PendingApproval", "Approved", "Rejected"
    public string RequestedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBillingAdjustmentDto
{
    public Guid InvoiceId { get; set; }
    public string Type { get; set; } = "Discount";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReviewBillingAdjustmentDto
{
    public string Status { get; set; } = "Approved"; // "Approved", "Rejected"
    public string? Notes { get; set; }
}

public class ExpenseTransactionDto
{
    public Guid Id { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string InvoiceReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string Status { get; set; } = "PendingApproval";
    public Guid? PharmacyStockRequisitionId { get; set; }
    public string? PharmacyRequisitionNumber { get; set; }
    public string RecordedByUserName { get; set; } = string.Empty;
    public string? ApprovedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateExpenseTransactionDto
{
    public string Category { get; set; } = "PharmaceuticalSupplier";
    public string VendorName { get; set; } = string.Empty;
    public string InvoiceReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public Guid? PharmacyStockRequisitionId { get; set; }
    public string? Notes { get; set; }
}

public class SettleExpenseTransactionDto
{
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}

public class PettyCashVoucherDto
{
    public Guid Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string Type { get; set; } = "Disbursement"; // "Disbursement", "Replenishment"
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Payee { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid DisbursedByUserId { get; set; }
    public string DisbursedByUserName { get; set; } = string.Empty;
    public decimal RemainingBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReceiptRef { get; set; }
}

public class CreatePettyCashVoucherDto
{
    public string Type { get; set; } = "Disbursement"; // "Disbursement" or "Replenishment"
    public decimal Amount { get; set; }
    public string Category { get; set; } = "OfficeSupplies";
    public string Payee { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ReceiptRef { get; set; }
}

public class PayrollRecordDto
{
    public Guid Id { get; set; }
    public string PayrollNumber { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal BaseSalary { get; set; }
    public int ConsultationsCount { get; set; }
    public decimal DoctorCommissionShare { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal TaxDeductions { get; set; }
    public decimal NetSalaryPayable { get; set; }
    public string Status { get; set; } = "Draft"; // "Draft", "Approved", "Disbursed"
    public DateTime? DisbursedAt { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GenerateMonthlyPayrollDto
{
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
}

public class DisbursePayrollDto
{
    public string PaymentReference { get; set; } = string.Empty;
}

public class AuditDailyCashReconciliationDto
{
    public string AuditStatus { get; set; } = "VerifiedBalanced"; // "VerifiedBalanced", "DiscrepancyApproved", "Disputed"
    public string? AuditNotes { get; set; }
}
