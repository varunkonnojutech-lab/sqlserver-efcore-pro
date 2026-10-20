using FluentAssertions;
using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;
using SqlServerEfCorePro.Data.Repositories;
using Xunit;

namespace SqlServerEfCorePro.Tests;

public sealed class OrderReadRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly OrderReadRepository _repository;

    public OrderReadRepositoryTests()
    {
        _database.Seed();
        _repository = new OrderReadRepository(_database.Context);
    }

    [Fact]
    public async Task SearchAsync_Filters_Projects_And_Pages()
    {
        var page1 = await _repository.SearchAsync(new OrderSearchRequest(
            CustomerId: _database.AdaId,
            Status: OrderStatus.Paid,
            Page: 1,
            PageSize: 1));
        var allPaid = await _repository.SearchAsync(new OrderSearchRequest(
            Status: OrderStatus.Paid,
            Page: 1,
            PageSize: 10));

        page1.TotalCount.Should().Be(1);
        page1.Items.Should().ContainSingle();
        page1.Items[0].ItemCount.Should().Be(2);
        page1.Items[0].TotalAmount.Should().Be(400m);
        allPaid.TotalCount.Should().Be(2);
        allPaid.Items.Should().OnlyContain(item => item.Status == OrderStatus.Paid);
    }

    [Fact]
    public async Task SearchAsync_Rejects_Page_Size_Over_The_Limit()
    {
        var act = () => _repository.SearchAsync(new OrderSearchRequest(PageSize: 101));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetCustomerHistoryAsync_Loads_The_Complete_Graph()
    {
        var orders = await _repository.GetCustomerHistoryAsync(_database.AdaId);

        orders.Should().HaveCount(3);
        orders.Should().OnlyContain(order => order.Customer.Email == "ada@example.com");
        orders.SelectMany(order => order.Items)
            .Should().OnlyContain(item => item.Product.Name.Length > 0);
        orders.Sum(order => order.Items.Count).Should().Be(4);
    }

    [Fact]
    public async Task Naive_And_Fixed_Loaders_Return_The_Same_Orders()
    {
        var naive = await _repository.LoadCustomerHistoryNaiveAsync(_database.AdaId);
        var fixedGraph = await _repository.GetCustomerHistoryAsync(_database.AdaId);

        naive.Select(order => order.Id)
            .Should().Equal(fixedGraph.Select(order => order.Id));
    }

    [Fact]
    public async Task GetCompiledCustomerOrdersAsync_Returns_Most_Recent_Summaries()
    {
        var orders = await _repository.GetCompiledCustomerOrdersAsync(_database.AdaId, take: 2);

        orders.Should().HaveCount(2);
        orders[0].OrderDateUtc.Should().BeAfter(orders[1].OrderDateUtc);
        orders[0].ItemCount.Should().Be(2);
        orders[0].TotalAmount.Should().Be(400m);
    }

    [Fact]
    public async Task GetTopCustomersFromStoredProcedureAsync_Validates_The_Date_Range()
    {
        var from = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var act = () => _repository.GetTopCustomersFromStoredProcedureAsync(from, from, 5);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    public void Dispose() => _database.Dispose();
}
