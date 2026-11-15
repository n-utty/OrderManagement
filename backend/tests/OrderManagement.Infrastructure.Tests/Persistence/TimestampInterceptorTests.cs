using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Infrastructure.Tests.Support;

namespace OrderManagement.Infrastructure.Tests.Persistence;

public class TimestampInterceptorTests
{
    [Fact]
    public async Task Adding_an_order_sets_CreatedAt_as_utc_and_leaves_UpdatedAt_empty()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var order = TestOrders.Order("PO-1");
        var expected = database.Clock.GetUtcNow().UtcDateTime;

        await using (var scope = database.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
            await repository.AddAsync(order);
            await repository.SaveChangesAsync();
        }

        await using (var scope = database.CreateScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>().GetByIdAsync(order.Id);

            Assert.NotNull(stored);
            Assert.Equal(expected, stored.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
            Assert.Null(stored.UpdatedAt);
        }
    }

    [Fact]
    public async Task Modifying_an_order_sets_UpdatedAt_and_keeps_CreatedAt()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var order = TestOrders.Order("PO-1");
        var createdAt = database.Clock.GetUtcNow().UtcDateTime;

        await using (var scope = database.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
            await repository.AddAsync(order);
            await repository.SaveChangesAsync();
        }

        database.Clock.Advance(TimeSpan.FromHours(1));
        var updatedAt = database.Clock.GetUtcNow().UtcDateTime;

        await using (var scope = database.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>();
            var stored = await repository.GetWithLineItemsAsync(order.Id);
            stored!.Status = OrderStatus.Confirmed;
            repository.Update(stored);
            await repository.SaveChangesAsync();
        }

        await using (var scope = database.CreateScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>().GetByIdAsync(order.Id);

            Assert.NotNull(stored);
            Assert.Equal(createdAt, stored.CreatedAt);
            Assert.Equal(updatedAt, stored.UpdatedAt);
            Assert.Equal(DateTimeKind.Utc, stored.UpdatedAt!.Value.Kind);
        }
    }
}
