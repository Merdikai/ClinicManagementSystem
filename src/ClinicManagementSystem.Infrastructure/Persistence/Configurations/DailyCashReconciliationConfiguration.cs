using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class DailyCashReconciliationConfiguration : IEntityTypeConfiguration<DailyCashReconciliation>
{
    public void Configure(EntityTypeBuilder<DailyCashReconciliation> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Shift)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Notes)
            .HasMaxLength(1000);

        builder.HasOne(d => d.ReceptionistUser)
            .WithMany()
            .HasForeignKey(d => d.ReceptionistUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
