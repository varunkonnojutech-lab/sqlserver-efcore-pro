using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlServerEfCorePro.Data.Entities;

namespace SqlServerEfCorePro.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Sku).HasMaxLength(40).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(160).IsRequired();
        builder.Property(product => product.Category).HasMaxLength(80).IsRequired();
        builder.Property(product => product.UnitPrice).HasPrecision(18, 2);

        builder.HasIndex(product => product.Sku)
            .IsUnique()
            .HasDatabaseName("UX_Products_Sku");

        // Product browsing normally filters by category and then sorts/searches
        // by name, so the key order follows the query rather than the table.
        builder.HasIndex(product => new { product.Category, product.Name })
            .HasDatabaseName("IX_Products_Category_Name");
    }
}
