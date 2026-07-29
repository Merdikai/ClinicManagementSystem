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
    }
}