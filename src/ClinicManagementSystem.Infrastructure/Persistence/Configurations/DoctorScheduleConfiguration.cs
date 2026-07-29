using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.HasKey(ds => ds.Id);

        builder.HasOne(ds => ds.Doctor)
            .WithMany()
            .HasForeignKey(ds => ds.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ds => ds.SlotDurationMinutes)
            .IsRequired();
    }
}