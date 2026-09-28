using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class StaffDutyShiftConfiguration : IEntityTypeConfiguration<StaffDutyShift>
{
    public void Configure(EntityTypeBuilder<StaffDutyShift> builder)
    {
        builder.ToTable("StaffDutyShifts");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Role).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Department).IsRequired().HasMaxLength(100);
        builder.Property(s => s.ShiftType).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.HandoverNotes).HasMaxLength(2000);
        builder.HasIndex(s => new { s.UserId, s.IsActive });
    }
}
