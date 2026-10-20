using Microsoft.EntityFrameworkCore;
using SqlServerEfCorePro.Data.Data;
using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;

namespace SqlServerEfCorePro.Data.Seeding;

public sealed class CommerceDbSeeder
{
    private readonly CommerceDbContext _db;

    public CommerceDbSeeder(CommerceDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Customers.AnyAsync(cancellationToken))
            return;

        var customers = new[]
        {
            new Customer { FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", CreatedUtc = Utc(2026, 1, 4) },
            new Customer { FirstName = "Grace", LastName = "Hopper", Email = "grace@example.com", CreatedUtc = Utc(2026, 1, 18) },
            new Customer { FirstName = "Katherine", LastName = "Johnson", Email = "katherine@example.com", CreatedUtc = Utc(2026, 2, 2) },
            new Customer { FirstName = "Alan", LastName = "Turing", Email = "alan@example.com", CreatedUtc = Utc(2026, 2, 21) }
        };

        var products = new[]
        {
            new Product { Sku = "KB-100", Name = "Mechanical Keyboard", Category = "Accessories", UnitPrice = 129.99m, StockQuantity = 140 },
            new Product { Sku = "MN-240", Name = "27-inch 4K Monitor", Category = "Displays", UnitPrice = 449.00m, StockQuantity = 62 },
            new Product { Sku = "DK-310", Name = "USB-C Docking Station", Category = "Accessories", UnitPrice = 189.50m, StockQuantity = 95 },
            new Product { Sku = "HS-420", Name = "Noise Cancelling Headset", Category = "Audio", UnitPrice = 219.99m, StockQuantity = 110 },
            new Product { Sku = "CH-510", Name = "Ergonomic Chair", Category = "Furniture", UnitPrice = 599.00m, StockQuantity = 28 }
        };

        _db.Customers.AddRange(customers);
        _db.Products.AddRange(products);
        await _db.SaveChangesAsync(cancellationToken);

        var orders = new[]
        {
            CreateOrder(customers[0], Utc(2026, 9, 4), OrderStatus.Shipped,
                (products[0], 1), (products[2], 1)),
            CreateOrder(customers[0], Utc(2026, 10, 2), OrderStatus.Paid,
                (products[3], 2)),
            CreateOrder(customers[1], Utc(2026, 9, 19), OrderStatus.Shipped,
                (products[1], 2), (products[2], 1)),
            CreateOrder(customers[1], Utc(2026, 10, 9), OrderStatus.Packed,
                (products[0], 1), (products[3], 1)),
            CreateOrder(customers[2], Utc(2026, 8, 27), OrderStatus.Shipped,
                (products[4], 1), (products[0], 1)),
            CreateOrder(customers[3], Utc(2026, 10, 12), OrderStatus.Pending,
                (products[1], 1), (products[3], 1)),
            CreateOrder(customers[2], Utc(2026, 10, 15), OrderStatus.Cancelled,
                (products[2], 1))
        };

        _db.Orders.AddRange(orders);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Order CreateOrder(
        Customer customer,
        DateTime orderDateUtc,
        OrderStatus status,
        params (Product Product, int Quantity)[] lines)
    {
        var order = new Order
        {
            Customer = customer,
            OrderDateUtc = orderDateUtc,
            Status = status
        };

        foreach (var (product, quantity) in lines)
        {
            order.Items.Add(new OrderItem
            {
                Order = order,
                Product = product,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = OrderTotalCalculator.CalculateLineTotal(quantity, product.UnitPrice)
            });
        }

        order.TotalAmount = OrderTotalCalculator.CalculateOrderTotal(order.Items);
        return order;
    }

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 12, 0, 0, DateTimeKind.Utc);
}
