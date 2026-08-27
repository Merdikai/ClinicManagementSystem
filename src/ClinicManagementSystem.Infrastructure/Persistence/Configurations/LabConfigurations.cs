using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class LabTestTemplateConfiguration : IEntityTypeConfiguration<LabTestTemplate>
{
    public void Configure(EntityTypeBuilder<LabTestTemplate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TestCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TestName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SampleType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Price).HasPrecision(18, 2);
    }
}

public class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalCost).HasPrecision(18, 2);

        builder.HasOne(e => e.Patient)
            .WithMany()
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Doctor)
            .WithMany()
            .HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.LabOrder)
            .HasForeignKey(i => i.LabOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
{
    public void Configure(EntityTypeBuilder<LabOrderItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Price).HasPrecision(18, 2);

        builder.HasOne(e => e.LabTestTemplate)
            .WithMany(t => t.OrderItems)
            .HasForeignKey(e => e.LabTestTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Result)
            .WithOne(r => r.LabOrderItem)
            .HasForeignKey<LabResult>(r => r.LabOrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
