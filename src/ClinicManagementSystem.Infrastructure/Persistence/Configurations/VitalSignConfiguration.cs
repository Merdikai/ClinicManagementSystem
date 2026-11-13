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
            .WithMany(a => a.VitalSigns)
            .HasForeignKey(vs => vs.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vs => vs.RecordedByUser)
            .WithMany()
            .HasForeignKey(vs => vs.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(vs => vs.Consultation)
            .WithMany()
            .HasForeignKey(vs => vs.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(vs => !vs.Appointment.IsDeleted && !vs.Appointment.Patient.IsDeleted);
    }
}
