using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MedicalRecordNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.MedicalRecordNumber)
            .IsUnique();

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Gender)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Phone)
            .HasMaxLength(20);

        builder.Property(p => p.Email)
            .HasMaxLength(255);

        builder.Property(p => p.Address)
            .HasMaxLength(500);

        builder.Property(p => p.BloodGroup)
            .HasMaxLength(5);

        builder.Property(p => p.EmergencyContact)
            .HasMaxLength(100);
    }
}