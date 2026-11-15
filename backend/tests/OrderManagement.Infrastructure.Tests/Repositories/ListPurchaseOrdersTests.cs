using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Infrastructure.Tests.Support;

namespace OrderManagement.Infrastructure.Tests.Repositories;

public class ListPurchaseOrdersTests
{
    [Fact]
    public async Task Lists_orders_newest_first_with_their_line_items()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await SeedAsync(database, ("PO-1", OrderStatus.Pending), ("PO-2", OrderStatus.Pending), ("PO-3", OrderStatus.Pending));

        await using var scope = database.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>()
            .ListAsync(status: null, page: 1, pageSize: 10);

        Assert.Equal(["PO-3", "PO-2", "PO-1"], result.Items.Select(order => order.ExternalReference));
        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, order => Assert.Single(order.LineItems));
    }

    [Fact]
    public async Task Filters_by_status_and_counts_only_matching_orders()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await SeedAsync(
            database,
            ("PO-1", OrderStatus.Pending),
            ("PO-2", OrderStatus.Confirmed),
            ("PO-3", OrderStatus.Pending),
            ("PO-4", OrderStatus.Cancelled));

        await using var scope = database.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>()
            .ListAsync(OrderStatus.Pending, page: 1, pageSize: 10);

        Assert.Equal(["PO-3", "PO-1"], result.Items.Select(order => order.ExternalReference));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Pages_through_orders_and_reports_the_full_total()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await SeedAsync(
            database,
            ("PO-1", OrderStatus.Pending),
            ("PO-2", OrderStatus.Pending),
            ("PO-3", OrderStatus.Pending),
            ("PO-4", OrderStatus.Pending),
            ("PO-5", OrderStatus.Pending));

        await using var scope = database.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>()
            .ListAsync(status: null, page: 2, pageSize: 2);

        Assert.Equal(["PO-3", "PO-2"], result.Items.Select(order => order.ExternalReference));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    private static async Task SeedAsync(SqliteTestDatabase database, params (string Reference, OrderStatus Status)[] orders)
    {
        foreach (var (reference, status) in orders)
        {
            await using var scope = database.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
            await repository.AddAsync(TestOrders.Order(reference, status));
            await repository.SaveChangesAsync();

            database.Clock.Advance(TimeSpan.FromMinutes(1));
        }
    }
}
