using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class ControlledSubstanceLogConfiguration : IEntityTypeConfiguration<ControlledSubstanceLog>
{
    public void Configure(EntityTypeBuilder<ControlledSubstanceLog> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.LogNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.LogNumber)
            .IsUnique();

        builder.Property(c => c.PrescribingDoctorName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.WitnessPharmacistName)
            .HasMaxLength(150);

        builder.Property(c => c.MedicalJustification)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(c => c.Medicine)
            .WithMany()
            .HasForeignKey(c => c.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Prescription)
            .WithMany()
            .HasForeignKey(c => c.PrescriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.DispensingPharmacist)
            .WithMany()
            .HasForeignKey(c => c.DispensingPharmacistUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.Medicine.IsDeleted && !c.Patient.IsDeleted);
    }
}
