using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class RadiologyOrderConfiguration : IEntityTypeConfiguration<RadiologyOrder>
{
    public void Configure(EntityTypeBuilder<RadiologyOrder> builder)
    {
        builder.HasKey(ro => ro.Id);

        builder.Property(ro => ro.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(ro => ro.OrderNumber)
            .IsUnique();

        builder.Property(ro => ro.Modality)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ro => ro.ProcedureName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ro => ro.BodyPart)
            .HasMaxLength(100);

        builder.Property(ro => ro.ClinicalIndication)
            .HasMaxLength(500);

        builder.Property(ro => ro.Priority)
            .HasMaxLength(50)
            .HasDefaultValue("Routine");

        builder.Property(ro => ro.Status)
            .HasMaxLength(50)
            .HasDefaultValue("Ordered");

        builder.Property(ro => ro.Findings)
            .HasMaxLength(4000);

        builder.Property(ro => ro.Impression)
            .HasMaxLength(2000);

        builder.Property(ro => ro.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(ro => ro.RadiologistSignature)
            .HasMaxLength(200);

        builder.Property(ro => ro.DoctorSignOffNotes)
            .HasMaxLength(1000);

        builder.Property(ro => ro.AccessionNumber)
            .HasMaxLength(60);

        builder.HasIndex(ro => ro.AccessionNumber);

        builder.Property(ro => ro.RoomSuite)
            .HasMaxLength(100);

        builder.Property(ro => ro.EquipmentModel)
            .HasMaxLength(100);

        builder.Property(ro => ro.CriticalAlertNotes)
            .HasMaxLength(1000);

        builder.Property(ro => ro.ExposureParameters)
            .HasMaxLength(200);

        builder.Property(ro => ro.ContrastAgentName)
            .HasMaxLength(100);

        builder.Property(ro => ro.ContrastLotNumber)
            .HasMaxLength(50);

        builder.Property(ro => ro.ContrastRoute)
            .HasMaxLength(50);

        builder.Property(ro => ro.SecondaryAttachmentUrls)
            .HasMaxLength(4000);

        builder.Property(ro => ro.RadiationDoseDlp)
            .HasPrecision(18, 2);

        builder.Property(ro => ro.RadiationDoseDap)
            .HasPrecision(18, 2);

        builder.Property(ro => ro.ContrastVolumeMl)
            .HasPrecision(18, 2);

        builder.HasOne(ro => ro.Patient)
            .WithMany()
            .HasForeignKey(ro => ro.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ro => ro.Doctor)
            .WithMany()
            .HasForeignKey(ro => ro.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ro => ro.Appointment)
            .WithMany()
            .HasForeignKey(ro => ro.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(ro => ro.Status);
        builder.HasIndex(ro => ro.Modality);
        builder.HasIndex(ro => ro.OrderedAt);
        builder.HasIndex(ro => ro.IsCritical);

        builder.HasQueryFilter(ro => !ro.Patient.IsDeleted);
    }
}
