using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlServerEfCorePro.Data.Entities;

namespace SqlServerEfCorePro.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderDateUtc).HasColumnType("datetime2");
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(order => order.TotalAmount).HasPrecision(18, 2);

        builder.HasOne(order => order.Customer)
            .WithMany(customer => customer.Orders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer order history is the hot path in this lab: equality on
        // CustomerId, then a range/order-by on OrderDateUtc.
        builder.HasIndex(order => new { order.CustomerId, order.OrderDateUtc })
            .HasDatabaseName("IX_Orders_CustomerId_OrderDateUtc");

        // Operational dashboards slice the same table a different way.
        builder.HasIndex(order => new { order.Status, order.OrderDateUtc })
            .HasDatabaseName("IX_Orders_Status_OrderDateUtc");
    }
}
