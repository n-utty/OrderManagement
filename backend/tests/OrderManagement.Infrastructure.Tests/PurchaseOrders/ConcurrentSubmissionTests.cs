using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Infrastructure.Tests.Support;

namespace OrderManagement.Infrastructure.Tests.PurchaseOrders;

public class ConcurrentSubmissionTests
{
    private const int ConcurrentCalls = 10;

    [Fact]
    public async Task Concurrent_submissions_of_the_same_request_create_exactly_one_order()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var request = TestOrders.Request("PO-RACE-1");
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Each call gets its own scope, and so its own DbContext and connection, like separate HTTP requests.
        var calls = Enumerable.Range(0, ConcurrentCalls)
            .Select(_ => Task.Run(async () =>
            {
                await start.Task;
                await using var scope = database.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<PurchaseOrderManager>().SubmitAsync(request);
            }))
            .ToList();

        start.SetResult();
        var results = await Task.WhenAll(calls);

        Assert.Single(results, result => result.WasCreated);
        Assert.Single(results.Select(result => result.Order.Id).Distinct());

        await using var verify = database.CreateScope();
        var stored = await verify.ServiceProvider.GetRequiredService<IPurchaseOrderRepository>()
            .ListAsync(status: null, page: 1, pageSize: 10);

        var order = Assert.Single(stored.Items);
        Assert.Equal(results[0].Order.Id, order.Id);
        Assert.Equal(2, order.LineItems.Count);
    }
}
