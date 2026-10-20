using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SqlServerEfCorePro.Data.Data;
using SqlServerEfCorePro.Data.Queries;
using SqlServerEfCorePro.Data.Repositories;
using SqlServerEfCorePro.Data.Seeding;

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "fixed";
var customerId = ReadIntArgument(args, "--customer", 1);

var connectionString = Environment.GetEnvironmentVariable("COMMERCE_CONNECTION_STRING")
    ?? "Server=localhost,14333;Database=CommercePro;User ID=sa;Password=LocalDev!123;TrustServerCertificate=True;Encrypt=False";

var options = new DbContextOptionsBuilder<CommerceDbContext>()
    .UseSqlServer(connectionString)
    // Command logging is noisy in production, but it is exactly what you want
    // in this lab: it makes the N+1 problem visible instead of theoretical.
    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
    .EnableSensitiveDataLogging()
    .Options;

await using var db = new CommerceDbContext(options);
var repository = new OrderReadRepository(db);
var seeder = new CommerceDbSeeder(db);

try
{
    switch (command)
    {
        case "migrate":
            await db.Database.MigrateAsync();
            Console.WriteLine("Migrations applied.");
            break;

        case "seed":
            await db.Database.MigrateAsync();
            await seeder.SeedAsync();
            Console.WriteLine("Seed data is ready.");
            break;

        case "naive":
            await Timed("NAIVE history (watch the SQL count)", async () =>
            {
                var orders = await repository.LoadCustomerHistoryNaiveAsync(customerId);
                PrintOrders(orders.Count, orders.Sum(order => order.Items.Count));
            });
            break;

        case "fixed":
            await Timed("FIXED history (split query + Include)", async () =>
            {
                var orders = await repository.GetCustomerHistoryAsync(customerId);
                PrintOrders(orders.Count, orders.Sum(order => order.Items.Count));
            });
            break;

        case "search":
            await Timed("Paged projection", async () =>
            {
                var page = await repository.SearchAsync(new OrderSearchRequest(
                    CustomerId: customerId,
                    Page: 1,
                    PageSize: 10));
                Console.WriteLine($"Orders: {page.Items.Count}; total matching: {page.TotalCount}");
                foreach (var order in page.Items)
                    Console.WriteLine($"  #{order.OrderId} {order.OrderDateUtc:yyyy-MM-dd} {order.Status} {order.TotalAmount:C}");
            });
            break;

        case "compiled":
            await Timed("Compiled query", async () =>
            {
                var orders = await repository.GetCompiledCustomerOrdersAsync(customerId, 10);
                PrintOrders(orders.Count, orders.Sum(order => order.ItemCount));
            });
            break;

        case "stored-procedure":
            await Timed("Stored procedure: top customers", async () =>
            {
                var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var rows = await repository.GetTopCustomersFromStoredProcedureAsync(from, from.AddYears(1), 5);
                foreach (var row in rows)
                    Console.WriteLine($"  {row.FullName}: {row.OrderCount} orders, {row.TotalSpent:C}");
            });
            break;

        case "restock":
            var productId = ReadIntArgument(args, "--product", 1);
            var quantity = ReadIntArgument(args, "--quantity", 5);
            var affected = await repository.RestockProductWithStoredProcedureAsync(productId, quantity);
            Console.WriteLine($"Rows affected: {affected}");
            break;

        default:
            PrintUsage();
            break;
    }
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("The demo could not reach or use SQL Server.");
    Console.Error.WriteLine("Start it with `docker compose up -d sqlserver`, then run `dotnet run --project src/SqlServerEfCorePro.Demo -- seed`.");
    Console.Error.WriteLine($"Details: {ex.Message}");
    return 1;
}

return 0;

static async Task Timed(string title, Func<Task> action)
{
    Console.WriteLine();
    Console.WriteLine($"== {title} ==");
    var stopwatch = Stopwatch.StartNew();
    await action();
    stopwatch.Stop();
    Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
}

static void PrintOrders(int orderCount, int itemCount) =>
    Console.WriteLine($"Loaded {orderCount} orders and {itemCount} order lines.");

static int ReadIntArgument(string[] args, string name, int fallback)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var value)
        ? value
        : fallback;
}

static void PrintUsage()
{
    Console.WriteLine("Commands: migrate | seed | naive | fixed | search | compiled | stored-procedure | restock");
    Console.WriteLine("Options: --customer <id> --product <id> --quantity <count>");
}
