using SqlServerEfCorePro.Api.Contracts;
using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;
using SqlServerEfCorePro.Data.Repositories;

namespace SqlServerEfCorePro.Api;

public static class CommerceEndpoints
{
    public static IEndpointRouteBuilder MapCommerceEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Commerce queries");

        api.MapGet("/orders/search", SearchOrdersAsync)
            .WithName("SearchOrders")
            .WithSummary("Paged, projected, no-tracking order search.");

        api.MapGet("/customers/{customerId:int}/orders/history", GetOrderHistoryAsync)
            .WithName("GetCustomerOrderHistory")
            .WithSummary("Complete order graph loaded without the N+1 pattern.");

        api.MapGet("/customers/{customerId:int}/orders/recent", GetCompiledOrdersAsync)
            .WithName("GetCompiledCustomerOrders")
            .WithSummary("Recent customer orders through an EF compiled query.");

        api.MapGet("/analytics/top-customers", GetTopCustomersAsync)
            .WithName("GetTopCustomers")
            .WithSummary("Stored-procedure report: dbo.usp_GetTopCustomers.");

        api.MapPost("/products/{productId:int}/restock", RestockProductAsync)
            .WithName("RestockProduct")
            .WithSummary("Stored-procedure update: dbo.usp_RestockProduct.");

        return app;
    }

    private static async Task<IResult> SearchOrdersAsync(
        int? customerId,
        OrderStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int? page,
        int? pageSize,
        IOrderReadRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await repository.SearchAsync(
                new OrderSearchRequest(
                    customerId,
                    status,
                    fromUtc,
                    toUtc,
                    page ?? 1,
                    pageSize ?? 20),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }

    private static async Task<IResult> GetOrderHistoryAsync(
        int customerId,
        IOrderReadRepository repository,
        CancellationToken cancellationToken)
    {
        var orders = await repository.GetCustomerHistoryAsync(customerId, cancellationToken);
        if (orders.Count == 0)
            return Results.NotFound(new ErrorResponse("No orders were found for that customer."));

        var response = orders.Select(order => new OrderHistoryResponse(
            order.Id,
            order.OrderDateUtc,
            order.Status,
            order.Customer.FullName,
            order.Customer.Email,
            order.TotalAmount,
            order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemResponse(
                    item.Product.Sku,
                    item.Product.Name,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal))
                .ToList()))
            .ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetCompiledOrdersAsync(
        int customerId,
        int? take,
        IOrderReadRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await repository.GetCompiledCustomerOrdersAsync(
                customerId,
                take ?? 10,
                cancellationToken);
            return Results.Ok(orders);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }

    private static async Task<IResult> GetTopCustomersAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        int? take,
        IOrderReadRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var from = fromUtc ?? new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = toUtc ?? from.AddYears(1);
            var rows = await repository.GetTopCustomersFromStoredProcedureAsync(
                from,
                to,
                take ?? 5,
                cancellationToken);
            return Results.Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }

    private static async Task<IResult> RestockProductAsync(
        int productId,
        int? quantity,
        IOrderReadRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var amount = quantity ?? 1;
            var rows = await repository.RestockProductWithStoredProcedureAsync(
                productId,
                amount,
                cancellationToken);
            return rows == 0
                ? Results.NotFound(new ErrorResponse("Product was not found."))
                : Results.Ok(new RestockResponse(productId, amount, rows));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }
}
