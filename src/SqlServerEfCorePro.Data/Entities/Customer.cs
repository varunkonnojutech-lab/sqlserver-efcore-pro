namespace SqlServerEfCorePro.Data.Entities;

public sealed class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public string FullName => $"{FirstName} {LastName}";
}
