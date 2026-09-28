using System;

namespace ClinicManagementSystem.Domain.Entities;

public class PayrollRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PayrollNumber { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public User StaffUser { get; set; } = null!;
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
    public string Status { get; set; } = "Draft"; // Draft, Approved, Disbursed
    public DateTime? DisbursedAt { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
