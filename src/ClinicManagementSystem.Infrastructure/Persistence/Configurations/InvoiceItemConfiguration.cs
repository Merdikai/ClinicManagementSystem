using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.HasKey(ii => ii.Id);

        builder.HasOne(ii => ii.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ii => ii.Description)
            .IsRequired()
            .HasMaxLength(500);

        // LineTotal is computed
        builder.Ignore(ii => ii.LineTotal);
    }
}