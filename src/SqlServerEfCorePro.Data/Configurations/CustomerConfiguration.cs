using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlServerEfCorePro.Data.Entities;

namespace SqlServerEfCorePro.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.FirstName).HasMaxLength(80).IsRequired();
        builder.Property(customer => customer.LastName).HasMaxLength(80).IsRequired();
        builder.Property(customer => customer.Email).HasMaxLength(256).IsRequired();
        builder.Property(customer => customer.CreatedUtc).HasColumnType("datetime2");

        builder.HasIndex(customer => customer.Email)
            .IsUnique()
            .HasDatabaseName("UX_Customers_Email");
    }
}
