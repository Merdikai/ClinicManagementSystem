using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class EmrAccessAuditLogConfiguration : IEntityTypeConfiguration<EmrAccessAuditLog>
{
    public void Configure(EntityTypeBuilder<EmrAccessAuditLog> builder)
    {
        builder.ToTable("EmrAccessAuditLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.UserName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.UserRole).IsRequired().HasMaxLength(100);
        builder.Property(l => l.AccessType).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Reason).HasMaxLength(1000);
        builder.Property(l => l.IpAddress).HasMaxLength(100);
        builder.HasOne(l => l.Patient)
            .WithMany()
            .HasForeignKey(l => l.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => l.PatientId);
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.Timestamp);

        builder.HasQueryFilter(l => !l.Patient.IsDeleted);
    }
}
