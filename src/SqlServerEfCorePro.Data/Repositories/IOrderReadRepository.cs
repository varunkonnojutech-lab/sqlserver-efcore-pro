using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;

namespace SqlServerEfCorePro.Data.Repositories;

public interface IOrderReadRepository
{
    /// <summary>Intentionally inefficient teaching example: one query for orders, then more queries while walking the graph.</summary>
    Task<IReadOnlyList<Order>> LoadCustomerHistoryNaiveAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>Production pattern: no tracking, one logical graph query, and a split query for the collection.</summary>
    Task<IReadOnlyList<Order>> GetCustomerHistoryAsync(int customerId, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryDto>> SearchAsync(OrderSearchRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSummaryDto>> GetCompiledCustomerOrdersAsync(int customerId, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerSalesDto>> GetTopCustomersFromStoredProcedureAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> RestockProductWithStoredProcedureAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default);
}
