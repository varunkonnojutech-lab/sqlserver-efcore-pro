using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SqlServerEfCorePro.Data.Data;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without starting the API.
/// Override the local connection with COMMERCE_CONNECTION_STRING.
/// </summary>
public sealed class CommerceDbContextFactory : IDesignTimeDbContextFactory<CommerceDbContext>
{
    public CommerceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("COMMERCE_CONNECTION_STRING")
            ?? "Server=localhost,14333;Database=CommercePro;User ID=sa;Password=LocalDev!123;TrustServerCertificate=True;Encrypt=False";

        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseSqlServer(connectionString, sql => sql
                .MigrationsAssembly(typeof(CommerceDbContext).Assembly.FullName)
                .EnableRetryOnFailure())
            .Options;

        return new CommerceDbContext(options);
    }
}
