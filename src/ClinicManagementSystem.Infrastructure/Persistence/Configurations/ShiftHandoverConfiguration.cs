using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class ShiftHandoverConfiguration : IEntityTypeConfiguration<ShiftHandover>
{
    public void Configure(EntityTypeBuilder<ShiftHandover> builder)
    {
        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.ShiftType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sh => sh.Situation)
            .HasMaxLength(2000);

        builder.Property(sh => sh.Background)
            .HasMaxLength(2000);

        builder.Property(sh => sh.Assessment)
            .HasMaxLength(2000);

        builder.Property(sh => sh.Recommendation)
            .HasMaxLength(2000);

        builder.HasOne(sh => sh.NurseUser)
            .WithMany()
            .HasForeignKey(sh => sh.NurseUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
