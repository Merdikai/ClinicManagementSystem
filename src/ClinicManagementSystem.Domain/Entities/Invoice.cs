using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BalanceDue => TotalAmount - (Payments?.Sum(p => p.AmountPaid) ?? 0);

    public ICollection<InvoiceItem> LineItems { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}