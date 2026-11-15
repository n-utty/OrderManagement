using OrderManagement.Domain.Common.Exceptions;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.Tests.Fakes;

namespace OrderManagement.Domain.Tests.PurchaseOrders;

public class QueryPurchaseOrderTests
{
    private readonly InMemoryPurchaseOrderRepository _repository = new();
    private readonly PurchaseOrderManager _manager;

    public QueryPurchaseOrderTests() => _manager = TestData.CreateManager(_repository);

    [Fact]
    public async Task Get_returns_the_order_with_its_line_items()
    {
        var created = await _manager.SubmitAsync(TestData.ValidRequest());

        var order = await _manager.GetAsync(created.Order.Id);

        Assert.Equal("PO-1001", order.ExternalReference);
        Assert.Equal(2, order.LineItems.Count);
        Assert.Equal(TestData.ValidRequestTotal, order.Total);
    }

    [Fact]
    public async Task Get_unknown_order_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _manager.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task List_returns_newest_first_and_can_filter_by_status()
    {
        await _manager.SubmitAsync(TestData.ValidRequest("PO-1"));
        var second = await _manager.SubmitAsync(TestData.ValidRequest("PO-2"));
        await _manager.SubmitAsync(TestData.ValidRequest("PO-3"));
        _repository.Orders.Single(order => order.Id == second.Order.Id).Status = OrderStatus.Confirmed;

        var all = await _manager.ListAsync();
        var confirmed = await _manager.ListAsync(OrderStatus.Confirmed);

        Assert.Equal(["PO-3", "PO-2", "PO-1"], all.Items.Select(order => order.ExternalReference));
        Assert.Equal("PO-2", Assert.Single(confirmed.Items).ExternalReference);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-5, 0, 1, 1)]
    [InlineData(2, 500, 2, PurchaseOrderManager.MaxPageSize)]
    public async Task List_clamps_out_of_range_paging(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _manager.ListAsync(page: page, pageSize: pageSize);

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
    }
}
