using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class BillingAdjustmentConfiguration : IEntityTypeConfiguration<BillingAdjustment>
{
    public void Configure(EntityTypeBuilder<BillingAdjustment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdjustmentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.AdjustmentNumber)
            .IsUnique();

        builder.Property(a => a.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Amount)
            .HasPrecision(18, 2);

        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.HasOne(a => a.Invoice)
            .WithMany()
            .HasForeignKey(a => a.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.Invoice.IsDeleted && !a.Invoice.Patient.IsDeleted);
    }
}
