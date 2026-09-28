using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class PharmacyStockRequisitionConfiguration : IEntityTypeConfiguration<PharmacyStockRequisition>
{
    public void Configure(EntityTypeBuilder<PharmacyStockRequisition> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequisitionNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.RequisitionNumber)
            .IsUnique();

        builder.Property(r => r.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.SupplierName)
            .HasMaxLength(150);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Notes)
            .HasMaxLength(1000);

        builder.Property(r => r.EstimatedUnitCost)
            .HasPrecision(18, 2);

        builder.Property(r => r.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasOne(r => r.Medicine)
            .WithMany()
            .HasForeignKey(r => r.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequestedByUser)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ApprovedByUser)
            .WithMany()
            .HasForeignKey(r => r.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
