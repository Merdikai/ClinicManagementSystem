using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class NursingProcedureConfiguration : IEntityTypeConfiguration<NursingProcedure>
{
    public void Configure(EntityTypeBuilder<NursingProcedure> builder)
    {
        builder.HasKey(np => np.Id);

        builder.Property(np => np.ProcedureType)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(np => np.BodySite)
            .HasMaxLength(150);

        builder.Property(np => np.Notes)
            .HasMaxLength(1000);

        builder.Property(np => np.PatientTolerance)
            .HasMaxLength(100);

        builder.Property(np => np.SuppliesUsed)
            .HasMaxLength(500);

        builder.Property(np => np.Status)
            .HasMaxLength(50)
            .HasDefaultValue("Completed");

        builder.Property(np => np.Priority)
            .HasMaxLength(50)
            .HasDefaultValue("Routine");

        builder.Property(np => np.Price)
            .HasPrecision(18, 2);

        builder.HasOne(np => np.Patient)
            .WithMany()
            .HasForeignKey(np => np.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(np => np.Appointment)
            .WithMany()
            .HasForeignKey(np => np.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(np => np.OrderedByDoctor)
            .WithMany()
            .HasForeignKey(np => np.OrderedByDoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(np => np.PerformedByUser)
            .WithMany()
            .HasForeignKey(np => np.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasQueryFilter(np => !np.Patient.IsDeleted);
    }
}
