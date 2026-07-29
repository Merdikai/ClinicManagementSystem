using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.HasKey(pi => pi.Id);

        builder.HasOne(pi => pi.Prescription)
            .WithMany(p => p.PrescriptionItems)
            .HasForeignKey(pi => pi.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pi => pi.Medicine)
            .WithMany()
            .HasForeignKey(pi => pi.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.DosageInstructions)
            .HasMaxLength(500);

        // UnitPrice is decimal – set globally in DbContext
        // TotalPrice is computed, not mapped
        builder.Ignore(pi => pi.TotalPrice);
    }
}