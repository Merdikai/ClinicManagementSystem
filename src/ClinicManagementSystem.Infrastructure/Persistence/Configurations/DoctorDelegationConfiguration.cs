using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class DoctorDelegationConfiguration : IEntityTypeConfiguration<DoctorDelegation>
{
    public void Configure(EntityTypeBuilder<DoctorDelegation> builder)
    {
        builder.ToTable("DoctorDelegations");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DelegatingDoctorName).IsRequired().HasMaxLength(200);
        builder.Property(d => d.CoveringDoctorName).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Reason).IsRequired().HasMaxLength(500);
        builder.Property(d => d.Notes).HasMaxLength(1000);
        builder.HasIndex(d => new { d.OriginalDoctorId, d.IsActive });
        builder.HasIndex(d => new { d.CoveringDoctorId, d.IsActive });
    }
}
