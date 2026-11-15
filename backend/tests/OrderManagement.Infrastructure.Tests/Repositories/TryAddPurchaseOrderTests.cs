using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Infrastructure.Tests.Support;

namespace OrderManagement.Infrastructure.Tests.Repositories;

public class TryAddPurchaseOrderTests
{
    [Fact]
    public async Task Returns_true_and_saves_a_new_order()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var order = TestOrders.Order("PO-1");

        await using var scope = database.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();

        Assert.True(await repository.TryAddAsync(order));
        Assert.NotNull(await repository.GetByReferenceAsync("PO-1"));
    }

    [Fact]
    public async Task Returns_false_for_a_duplicate_reference_and_leaves_the_context_usable()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var original = TestOrders.Order("PO-1");

        await using (var scope = database.CreateScope())
        {
            Assert.True(await scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>().TryAddAsync(original));
        }

        await using (var scope = database.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();

            Assert.False(await repository.TryAddAsync(TestOrders.Order("PO-1")));

            // The rejected insert must not linger in the tracker and fail the next save.
            Assert.True(await repository.TryAddAsync(TestOrders.Order("PO-2")));

            var stored = await repository.GetByReferenceAsync("PO-1");
            Assert.Equal(original.Id, stored!.Id);

            var all = await repository.ListAsync(status: null, page: 1, pageSize: 10);
            Assert.Equal(2, all.TotalCount);
        }
    }
}
