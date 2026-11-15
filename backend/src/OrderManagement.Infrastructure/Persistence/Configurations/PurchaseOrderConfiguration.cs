using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Validators;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.ExternalReference)
            .HasMaxLength(CreatePurchaseOrderRequestValidator.ExternalReferenceMaxLength)
            .IsRequired();

        builder.Property(order => order.CustomerId)
            .HasMaxLength(CreatePurchaseOrderRequestValidator.CustomerIdMaxLength)
            .IsRequired();

        builder.Property(order => order.CustomerName)
            .HasMaxLength(CreatePurchaseOrderRequestValidator.CustomerNameMaxLength)
            .IsRequired();

        builder.Property(order => order.CustomerEmail)
            .HasMaxLength(CreatePurchaseOrderRequestValidator.CustomerEmailMaxLength)
            .IsRequired();

        builder.Property(order => order.Currency).HasMaxLength(3).IsRequired();

        builder.Property(order => order.Notes)
            .HasMaxLength(CreatePurchaseOrderRequestValidator.NotesMaxLength);

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(order => order.Subtotal).HasPrecision(18, 2);
        builder.Property(order => order.Total).HasPrecision(18, 2);

        // SHA-256 as upper-case hex.
        builder.Property(order => order.Fingerprint).HasMaxLength(64).IsRequired();

        builder.Property(order => order.CreatedAt).IsRequired();

        // The database, not the lookup in the manager, is what guarantees one order per reference
        // when two submissions race each other.
        builder.HasIndex(order => order.ExternalReference).IsUnique();

        builder.HasIndex(order => order.CreatedAt);
        builder.HasIndex(order => order.Status);

        builder.HasMany(order => order.LineItems)
            .WithOne()
            .HasForeignKey(item => item.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
