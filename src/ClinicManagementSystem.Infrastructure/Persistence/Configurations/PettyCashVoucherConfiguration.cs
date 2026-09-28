using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class PettyCashVoucherConfiguration : IEntityTypeConfiguration<PettyCashVoucher>
{
    public void Configure(EntityTypeBuilder<PettyCashVoucher> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VoucherNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(v => v.VoucherNumber)
            .IsUnique();

        builder.Property(v => v.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Payee)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(v => v.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.DisbursedByUserName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Amount)
            .HasPrecision(18, 2);

        builder.Property(v => v.RemainingBalance)
            .HasPrecision(18, 2);

        builder.Property(v => v.ReceiptRef)
            .HasMaxLength(100);
    }
}
