using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class VisitorInquiryConfiguration : IEntityTypeConfiguration<VisitorInquiry>
{
    public void Configure(EntityTypeBuilder<VisitorInquiry> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VisitorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(v => v.InquiryType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.DepartmentTarget)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Notes)
            .HasMaxLength(2000);

        builder.Property(v => v.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(v => v.HandledByUser)
            .WithMany()
            .HasForeignKey(v => v.HandledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
