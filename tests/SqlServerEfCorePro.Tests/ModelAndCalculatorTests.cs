using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SqlServerEfCorePro.Data.Data;
using SqlServerEfCorePro.Data.Entities;
using SqlServerEfCorePro.Data.Queries;
using Xunit;

namespace SqlServerEfCorePro.Tests;

public sealed class ModelAndCalculatorTests
{
    [Fact]
    public void Model_Contains_The_Tuning_Indexes_And_Keyless_Procedure_Result()
    {
        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;User ID=unused;Password=unused;TrustServerCertificate=True")
            .Options;

        using var context = new CommerceDbContext(options);
        var orderIndexes = context.Model.FindEntityType(typeof(Order))!
            .GetIndexes()
            .Select(index => index.GetDatabaseName())
            .ToList();
        var procedureResult = context.Model.FindEntityType(typeof(CustomerSalesDto));

        orderIndexes.Should().Contain("IX_Orders_CustomerId_OrderDateUtc");
        orderIndexes.Should().Contain("IX_Orders_Status_OrderDateUtc");
        procedureResult.Should().NotBeNull();
        procedureResult!.GetKeys().Should().BeEmpty();
    }

    [Fact]
    public void CalculateLineTotal_Multiplies_And_Rounds()
    {
        OrderTotalCalculator.CalculateLineTotal(3, 10.125m).Should().Be(30.38m);
    }

    [Fact]
    public void CalculateLineTotal_Rejects_A_NonPositive_Quantity()
    {
        var act = () => OrderTotalCalculator.CalculateLineTotal(0, 10m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
