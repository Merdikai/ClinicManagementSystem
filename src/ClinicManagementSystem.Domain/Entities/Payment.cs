namespace ClinicManagementSystem.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
    public string TransactionReference { get; set; } = string.Empty;
}