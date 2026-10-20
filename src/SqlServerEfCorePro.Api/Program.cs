using Microsoft.EntityFrameworkCore;
using SqlServerEfCorePro.Api;
using SqlServerEfCorePro.Data.Data;
using SqlServerEfCorePro.Data.Repositories;
using SqlServerEfCorePro.Data.Seeding;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CommerceDb")
    ?? throw new InvalidOperationException("ConnectionStrings:CommerceDb is not configured.");

builder.Services.AddDbContext<CommerceDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql
        .EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorNumbersToAdd: null)
        .CommandTimeout(30)));

builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();
builder.Services.AddScoped<CommerceDbSeeder>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
    await db.Database.MigrateAsync();
}

if (builder.Configuration.GetValue<bool>("Database:SeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<CommerceDbSeeder>();
    await seeder.SeedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "sqlserver-efcore-pro",
    utc = DateTime.UtcNow
}));

app.MapCommerceEndpoints();
app.Run();

public partial class Program
{
}
