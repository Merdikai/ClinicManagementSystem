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

    public decimal SubTotal
    {
        get;
        set => field = value < 0m
            ? throw new ArgumentOutOfRangeException(nameof(SubTotal), "SubTotal cannot be negative.")
            : value;
    }
    public decimal TaxAmount
    {
        get;
        set => field = value < 0m
            ? throw new ArgumentOutOfRangeException(nameof(TaxAmount), "Tax amount cannot be negative.")
            : value;
    }
    public decimal DiscountAmount
    {
        get;
        set => field = value < 0m
            ? throw new ArgumentOutOfRangeException(nameof(DiscountAmount), "Discount amount cannot be negative.")
            : value;
    }
    public decimal TotalAmount
    {
        get;
        set => field = value < 0m
            ? throw new ArgumentOutOfRangeException(nameof(TotalAmount), "Total amount cannot be negative.")
            : value;
    }
    public decimal BalanceDue => TotalAmount - (Payments?.Sum(p => p.AmountPaid) ?? 0);
    public string Department { get; set; } = "General"; // Consultation, Pharmacy, Laboratory, Nursing, General
    public string? BillingNotes { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public uint RowVersion { get; set; }

    public ICollection<InvoiceItem> LineItems { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}