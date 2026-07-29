using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        builder.HasOne(i => i.Patient)
            .WithMany(p => p.Invoices)
            .HasForeignKey(i => i.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Appointment)
            .WithMany()
            .HasForeignKey(i => i.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        // All monetary fields are decimal – set globally
        // BalanceDue is computed, not mapped
        builder.Ignore(i => i.BalanceDue);
    }
}