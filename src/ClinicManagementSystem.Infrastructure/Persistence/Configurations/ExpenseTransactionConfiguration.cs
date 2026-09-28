using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class ExpenseTransactionConfiguration : IEntityTypeConfiguration<ExpenseTransaction>
{
    public void Configure(EntityTypeBuilder<ExpenseTransaction> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExpenseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.ExpenseNumber)
            .IsUnique();

        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.VendorName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.InvoiceReference)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.PharmacyStockRequisition)
            .WithMany()
            .HasForeignKey(e => e.PharmacyStockRequisitionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
