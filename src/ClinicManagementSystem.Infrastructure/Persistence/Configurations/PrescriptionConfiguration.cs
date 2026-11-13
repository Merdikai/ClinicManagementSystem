using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Consultation)
            .WithOne(c => c.Prescription)
            .HasForeignKey<Prescription>(p => p.ConsultationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.VerificationNotes)
            .HasMaxLength(1000);

        builder.HasOne(p => p.DispensedByUser)
            .WithMany()
            .HasForeignKey(p => p.DispensedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Invoice)
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(p => !p.IsDeleted && !p.Consultation.Appointment.IsDeleted && !p.Consultation.Appointment.Patient.IsDeleted);

        builder.Property(p => p.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");
    }
}