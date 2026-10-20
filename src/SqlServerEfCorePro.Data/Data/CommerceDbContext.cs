using Microsoft.EntityFrameworkCore;
using SqlServerEfCorePro.Data.Configurations;
using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;

namespace SqlServerEfCorePro.Data.Data;

public sealed class CommerceDbContext : DbContext
{
    public CommerceDbContext(DbContextOptions<CommerceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    // Used only as the target of FromSql calls against the stored procedure.
    public DbSet<CustomerSalesDto> TopCustomerRows => Set<CustomerSalesDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());

        modelBuilder.Entity<CustomerSalesDto>(builder =>
        {
            builder.HasNoKey();
            builder.ToView(null);
            builder.Property(row => row.FullName).HasMaxLength(161);
            builder.Property(row => row.Email).HasMaxLength(256);
            builder.Property(row => row.TotalSpent).HasPrecision(18, 2);
        });
    }
}
