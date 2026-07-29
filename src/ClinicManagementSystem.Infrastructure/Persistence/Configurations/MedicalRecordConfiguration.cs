using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.HasKey(mr => mr.Id);

        builder.HasOne(mr => mr.Patient)
            .WithMany(p => p.MedicalRecords)
            .HasForeignKey(mr => mr.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mr => mr.Appointment)
            .WithOne(a => a.MedicalRecord)
            .HasForeignKey<MedicalRecord>(mr => mr.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}