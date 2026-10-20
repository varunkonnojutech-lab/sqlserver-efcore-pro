using SqlServerEfCorePro.Data.Entities;

namespace SqlServerEfCorePro.Api.Contracts;

public sealed record OrderHistoryResponse(
    int OrderId,
    DateTime OrderDateUtc,
    OrderStatus Status,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record RestockResponse(int ProductId, int Quantity, int RowsAffected);

public sealed record ErrorResponse(string Error);
