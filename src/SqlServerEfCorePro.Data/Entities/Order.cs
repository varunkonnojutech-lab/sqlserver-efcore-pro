namespace SqlServerEfCorePro.Data.Entities;

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Packed = 2,
    Shipped = 3,
    Cancelled = 4
}

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime OrderDateUtc { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
