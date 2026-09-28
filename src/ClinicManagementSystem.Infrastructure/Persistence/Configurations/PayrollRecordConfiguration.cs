using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PayrollNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.PayrollNumber)
            .IsUnique();

        builder.Property(p => p.StaffName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.BaseSalary)
            .HasPrecision(18, 2);

        builder.Property(p => p.DoctorCommissionShare)
            .HasPrecision(18, 2);

        builder.Property(p => p.OvertimeHours)
            .HasPrecision(18, 2);

        builder.Property(p => p.OvertimePay)
            .HasPrecision(18, 2);

        builder.Property(p => p.TaxDeductions)
            .HasPrecision(18, 2);

        builder.Property(p => p.NetSalaryPayable)
            .HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.PaymentReference)
            .HasMaxLength(100);

        builder.HasOne(p => p.StaffUser)
            .WithMany()
            .HasForeignKey(p => p.StaffUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
