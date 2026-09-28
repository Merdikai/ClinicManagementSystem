using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(m => m.Code)
            .IsUnique();

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.GenericName)
            .HasMaxLength(200);

        builder.Property(m => m.Category)
            .HasMaxLength(100);

        builder.Property(m => m.DosageForm)
            .HasMaxLength(50);

        builder.Property(m => m.Strength)
            .HasMaxLength(50);

        builder.Property(m => m.Manufacturer)
            .HasMaxLength(150);

        builder.Property(m => m.BatchNumber)
            .HasMaxLength(100);

        builder.Property(m => m.StorageConditions)
            .HasMaxLength(150);

        builder.Property(m => m.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(m => m.UnitPrice)
            .HasPrecision(18, 2);
    }
}