using SqlServerEfCorePro.Data.Entities;

namespace SqlServerEfCorePro.Data.Queries;

public static class OrderTotalCalculator
{
    public static decimal CalculateLineTotal(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        return Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateOrderTotal(IEnumerable<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Sum(item => item.LineTotal);
    }
}
