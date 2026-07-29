using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.Appointment)
            .WithOne(a => a.Consultation)
            .HasForeignKey<Consultation>(c => c.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Doctor)
            .WithMany()
            .HasForeignKey(c => c.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(c => c.Symptoms)
            .HasMaxLength(1000);

        builder.Property(c => c.Diagnosis)
            .HasMaxLength(1000);
    }
}