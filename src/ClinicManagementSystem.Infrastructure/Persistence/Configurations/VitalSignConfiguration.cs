using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class VitalSignConfiguration : IEntityTypeConfiguration<VitalSign>
{
    public void Configure(EntityTypeBuilder<VitalSign> builder)
    {
        builder.HasKey(vs => vs.Id);

        builder.HasOne(vs => vs.Appointment)
            .WithOne(a => a.VitalSign)
            .HasForeignKey<VitalSign>(vs => vs.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vs => vs.Nurse)
            .WithMany()
            .HasForeignKey(vs => vs.RecordedByNurseId)
            .OnDelete(DeleteBehavior.Restrict);

        // All vitals are decimal – precision set globally in DbContext
    }
}