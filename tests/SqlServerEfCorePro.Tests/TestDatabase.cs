using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlServerEfCorePro.Data.Data;
using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;

namespace SqlServerEfCorePro.Tests;

/// <summary>
/// The production provider is SQL Server. Repository tests use SQLite's
/// relational provider because it can translate the same LINQ in-process;
/// stored procedures and execution plans remain SQL Server integration topics
/// and are covered by the scripts/demo instead of being faked here.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection(
            $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new CommerceDbContext(options);
        Context.Database.EnsureCreated();
    }

    public CommerceDbContext Context { get; }
    public int AdaId { get; private set; }
    public int GraceId { get; private set; }

    public void Seed()
    {
        var ada = new Customer
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            CreatedUtc = Date(2026, 1, 3)
        };
        var grace = new Customer
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com",
            CreatedUtc = Date(2026, 1, 8)
        };
        var keyboard = new Product
        {
            Sku = "KB-100",
            Name = "Keyboard",
            Category = "Accessories",
            UnitPrice = 100m,
            StockQuantity = 10
        };
        var monitor = new Product
        {
            Sku = "MN-200",
            Name = "Monitor",
            Category = "Displays",
            UnitPrice = 300m,
            StockQuantity = 5
        };

        Context.AddRange(ada, grace, keyboard, monitor);
        Context.SaveChanges();
        AdaId = ada.Id;
        GraceId = grace.Id;

        Context.Orders.AddRange(
            CreateOrder(ada, Date(2026, 10, 3), OrderStatus.Paid,
                (keyboard, 1), (monitor, 1)),
            CreateOrder(ada, Date(2026, 9, 3), OrderStatus.Shipped,
                (keyboard, 2)),
            CreateOrder(ada, Date(2026, 8, 3), OrderStatus.Cancelled,
                (monitor, 1)),
            CreateOrder(grace, Date(2026, 10, 1), OrderStatus.Paid,
                (monitor, 1)));
        Context.SaveChanges();
    }

    private static Order CreateOrder(
        Customer customer,
        DateTime date,
        OrderStatus status,
        params (Product Product, int Quantity)[] lines)
    {
        var order = new Order
        {
            Customer = customer,
            OrderDateUtc = date,
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

    private static DateTime Date(int year, int month, int day) =>
        new(year, month, day, 12, 0, 0, DateTimeKind.Utc);

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
