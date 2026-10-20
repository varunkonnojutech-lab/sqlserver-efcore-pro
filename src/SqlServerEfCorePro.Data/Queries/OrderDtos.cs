using SqlServerEfCorePro.Data.Entities;

namespace SqlServerEfCorePro.Data.Queries;

public sealed class OrderSummaryDto
{
    public int OrderId { get; set; }
    public DateTime OrderDateUtc { get; set; }
    public OrderStatus Status { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Shape returned by dbo.usp_GetTopCustomers. It is intentionally a keyless
/// EF type: a stored-procedure result is a report, not an entity with an
/// identity that EF should track.
/// </summary>
public sealed class CustomerSalesDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record OrderSearchRequest(
    int? CustomerId = null,
    OrderStatus? Status = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 20);
