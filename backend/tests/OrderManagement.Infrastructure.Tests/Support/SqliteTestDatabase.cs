using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.PurchaseOrders.Validators;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Tests.Support;

// A real SQLite database held in memory: constraints and indexes behave exactly as on disk.
// Shared-cache mode lets every scope open its own connection to the same database, the way
// concurrent requests would; the keep-alive connection stops it vanishing between scopes.
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly ServiceProvider _services;

    private SqliteTestDatabase(SqliteConnection keepAlive, ServiceProvider services, TestClock clock)
    {
        _keepAlive = keepAlive;
        _services = services;
        Clock = clock;
    }

    public TestClock Clock { get; }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connectionString = $"Data Source=orders-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync();

        var clock = new TestClock();

        // Registered first so AddInfrastructure's TryAdd keeps the test clock.
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(clock)
            .AddInfrastructure(connectionString)
            .AddScoped<IValidator<CreatePurchaseOrderRequest>, CreatePurchaseOrderRequestValidator>()
            .AddScoped<IValidator<ChangeOrderStatusRequest>, ChangeOrderStatusRequestValidator>()
            .AddScoped<PurchaseOrderManager>()
            .BuildServiceProvider();

        var database = new SqliteTestDatabase(keepAlive, services, clock);

        await using var scope = database.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

        return database;
    }

    public AsyncServiceScope CreateScope() => _services.CreateAsyncScope();

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _keepAlive.DisposeAsync();
    }
}
