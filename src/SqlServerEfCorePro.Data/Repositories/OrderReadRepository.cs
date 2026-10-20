using Microsoft.EntityFrameworkCore;
using SqlServerEfCorePro.Data.Data;
using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;

namespace SqlServerEfCorePro.Data.Repositories;

public sealed class OrderReadRepository : IOrderReadRepository
{
    private readonly CommerceDbContext _db;

    // A compiled query pays the LINQ translation cost once. It is most useful
    // for a stable, very hot query shape; do not compile every query by reflex.
    private static readonly Func<CommerceDbContext, int, int, IAsyncEnumerable<OrderSummaryDto>>
        CompiledCustomerOrders = EF.CompileAsyncQuery(
            (CommerceDbContext db, int customerId, int take) =>
                db.Orders
                    .AsNoTracking()
                    .Where(order => order.CustomerId == customerId)
                    .OrderByDescending(order => order.OrderDateUtc)
                    .Take(take)
                    .Select(order => new OrderSummaryDto
                    {
                        OrderId = order.Id,
                        OrderDateUtc = order.OrderDateUtc,
                        Status = order.Status,
                        ItemCount = order.Items.Count,
                        TotalAmount = order.TotalAmount
                    }));

    public OrderReadRepository(CommerceDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<Order>> LoadCustomerHistoryNaiveAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        // DO NOT copy this shape into production code. It is here so you can
        // run it with EF logging / SQL Profiler and watch the N+1 pattern:
        // 1 query for orders + 1 per order for items + 1 per item for product.
        var orders = await _db.Orders
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.OrderDateUtc)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            await _db.Entry(order)
                .Collection(item => item.Items)
                .LoadAsync(cancellationToken);

            foreach (var item in order.Items)
            {
                await _db.Entry(item)
                    .Reference(line => line.Product)
                    .LoadAsync(cancellationToken);
            }
        }

        return orders;
    }

    public async Task<IReadOnlyList<Order>> GetCustomerHistoryAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        // Read-only graph query:
        // - AsNoTracking avoids change-tracker snapshots for data we will not edit.
        // - Include loads the complete graph instead of lazy round trips.
        // - AsSplitQuery prevents one giant JOIN from repeating order/customer
        //   columns for every order line (cartesian explosion).
        return await _db.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .AsSplitQuery()
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.OrderDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<OrderSummaryDto>> SearchAsync(
        OrderSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Page < 1)
            throw new ArgumentOutOfRangeException(nameof(request), "Page must be at least 1.");
        if (request.PageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request), "Page size must be between 1 and 100.");
        if (request.FromUtc.HasValue && request.ToUtc.HasValue && request.ToUtc <= request.FromUtc)
            throw new ArgumentException("The end date must be later than the start date.", nameof(request));

        IQueryable<Order> query = _db.Orders.AsNoTracking();

        if (request.CustomerId.HasValue)
            query = query.Where(order => order.CustomerId == request.CustomerId.Value);
        if (request.Status.HasValue)
            query = query.Where(order => order.Status == request.Status.Value);
        if (request.FromUtc.HasValue)
            query = query.Where(order => order.OrderDateUtc >= request.FromUtc.Value);
        if (request.ToUtc.HasValue)
            query = query.Where(order => order.OrderDateUtc < request.ToUtc.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Project to the columns the caller needs. Do not materialize whole
        // entities for a grid/report that only displays five fields.
        var items = await query
            .OrderByDescending(order => order.OrderDateUtc)
            .ThenByDescending(order => order.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(order => new OrderSummaryDto
            {
                OrderId = order.Id,
                OrderDateUtc = order.OrderDateUtc,
                Status = order.Status,
                ItemCount = order.Items.Count,
                TotalAmount = order.TotalAmount
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetCompiledCustomerOrdersAsync(
        int customerId,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be between 1 and 100.");

        var results = new List<OrderSummaryDto>();
        await foreach (var row in CompiledCustomerOrders(_db, customerId, take))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(row);
        }

        return results;
    }

    public async Task<IReadOnlyList<CustomerSalesDto>> GetTopCustomersFromStoredProcedureAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (toUtc <= fromUtc)
            throw new ArgumentException("The end date must be later than the start date.", nameof(toUtc));
        if (take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be between 1 and 100.");

        // Parameters stay parameters. Never build this call by concatenating
        // caller input into a raw SQL string.
        return await _db.TopCustomerRows
            .FromSqlInterpolated(
                $"EXEC dbo.usp_GetTopCustomers @FromUtc = {fromUtc}, @ToUtc = {toUtc}, @Top = {take}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RestockProductWithStoredProcedureAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId), "Product id must be positive.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        return await _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.usp_RestockProduct @ProductId = {productId}, @Quantity = {quantity}",
            cancellationToken);
    }
}
