using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class LabReagentConfiguration : IEntityTypeConfiguration<LabReagent>
{
    public void Configure(EntityTypeBuilder<LabReagent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.CatalogCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LotNumber).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UnitOfMeasure).HasMaxLength(50).IsRequired();
        builder.Property(e => e.StorageCondition).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Manufacturer).HasMaxLength(150);
        builder.Property(e => e.UnitCost).HasPrecision(18, 2);
    }
}

public class LabStockRequisitionConfiguration : IEntityTypeConfiguration<LabStockRequisition>
{
    public void Configure(EntityTypeBuilder<LabStockRequisition> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RequisitionNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ItemName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UnitOfMeasure).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Urgency).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EstimatedUnitCost).HasPrecision(18, 2);
        builder.Property(e => e.TotalEstimatedCost).HasPrecision(18, 2);

        builder.HasOne(e => e.LabReagent)
            .WithMany()
            .HasForeignKey(e => e.LabReagentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.RequestedByUser)
            .WithMany()
            .HasForeignKey(e => e.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LabEquipmentLogConfiguration : IEntityTypeConfiguration<LabEquipmentLog>
{
    public void Configure(EntityTypeBuilder<LabEquipmentLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EquipmentName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EquipmentModel).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LogType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PerformedByUserName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.StandardDeviationScore).HasPrecision(18, 2);
    }
}
