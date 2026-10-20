using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlServerEfCorePro.Data.Entities;

namespace SqlServerEfCorePro.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.LineTotal).HasPrecision(18, 2);

        builder.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.OrderItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per product per order. The index also makes the normal
        // "load all lines for this order" lookup a seek instead of a scan.
        builder.HasIndex(item => new { item.OrderId, item.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_OrderItems_OrderId_ProductId");
    }
}
