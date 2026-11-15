using FluentValidation;
using OrderManagement.Domain.Common.Exceptions;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.Tests.Fakes;

namespace OrderManagement.Domain.Tests.PurchaseOrders;

public class ChangeOrderStatusTests
{
    public enum Outcome
    {
        NoOp,
        Allowed,
        Rejected,
    }

    private readonly InMemoryPurchaseOrderRepository _repository = new();
    private readonly PurchaseOrderManager _manager;

    public ChangeOrderStatusTests() => _manager = TestData.CreateManager(_repository);

    // Every from/to pair, so a change to the transition table can't slip through untested.
    public static TheoryData<OrderStatus, OrderStatus, Outcome> Matrix => new()
    {
        { OrderStatus.Pending, OrderStatus.Pending, Outcome.NoOp },
        { OrderStatus.Pending, OrderStatus.Confirmed, Outcome.Allowed },
        { OrderStatus.Pending, OrderStatus.Fulfilled, Outcome.Rejected },
        { OrderStatus.Pending, OrderStatus.Cancelled, Outcome.Allowed },

        { OrderStatus.Confirmed, OrderStatus.Pending, Outcome.Rejected },
        { OrderStatus.Confirmed, OrderStatus.Confirmed, Outcome.NoOp },
        { OrderStatus.Confirmed, OrderStatus.Fulfilled, Outcome.Allowed },
        { OrderStatus.Confirmed, OrderStatus.Cancelled, Outcome.Allowed },

        { OrderStatus.Fulfilled, OrderStatus.Pending, Outcome.Rejected },
        { OrderStatus.Fulfilled, OrderStatus.Confirmed, Outcome.Rejected },
        { OrderStatus.Fulfilled, OrderStatus.Fulfilled, Outcome.NoOp },
        { OrderStatus.Fulfilled, OrderStatus.Cancelled, Outcome.Rejected },

        { OrderStatus.Cancelled, OrderStatus.Pending, Outcome.Rejected },
        { OrderStatus.Cancelled, OrderStatus.Confirmed, Outcome.Rejected },
        { OrderStatus.Cancelled, OrderStatus.Fulfilled, Outcome.Rejected },
        { OrderStatus.Cancelled, OrderStatus.Cancelled, Outcome.NoOp },
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Transition_table_matches_the_matrix(OrderStatus from, OrderStatus to, Outcome expected)
    {
        Assert.Equal(expected == Outcome.Allowed, OrderStatusTransitions.CanTransition(from, to));
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Manager_applies_the_matrix(OrderStatus from, OrderStatus to, Outcome expected)
    {
        var order = await SeedOrder(from);
        var savesBefore = _repository.SaveChangesCount;

        var act = () => _manager.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest { Status = to });

        switch (expected)
        {
            case Outcome.NoOp:
                var unchanged = await act();
                Assert.Equal(from, unchanged.Status);
                Assert.Equal(savesBefore, _repository.SaveChangesCount);
                Assert.Null(order.UpdatedAt);
                break;

            case Outcome.Allowed:
                var changed = await act();
                Assert.Equal(to, changed.Status);
                Assert.Equal(to, order.Status);
                Assert.Equal(savesBefore + 1, _repository.SaveChangesCount);
                Assert.NotNull(changed.UpdatedAt);
                Assert.Equal(OrderStatusTransitions.AllowedFrom(to), changed.AllowedTransitions);
                break;

            case Outcome.Rejected:
                await Assert.ThrowsAsync<BusinessRuleException>(act);
                Assert.Equal(from, order.Status);
                Assert.Equal(savesBefore, _repository.SaveChangesCount);
                break;
        }
    }

    [Theory]
    [InlineData(OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.Fulfilled, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    public void Only_fulfilled_and_cancelled_are_final(OrderStatus status, bool isFinal)
    {
        Assert.Equal(isFinal, OrderStatusTransitions.IsFinal(status));
    }

    [Fact]
    public async Task Rejected_change_names_the_allowed_next_statuses()
    {
        var order = await SeedOrder(OrderStatus.Pending);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _manager.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest { Status = OrderStatus.Fulfilled }));

        Assert.Equal(
            "Order PO-1001 can't move from Pending to Fulfilled. Allowed next statuses: Confirmed or Cancelled.",
            error.Message);
        Assert.Equal("Pending", error.Details["currentStatus"]);
        Assert.Equal("Fulfilled", error.Details["requestedStatus"]);
        Assert.Equal(new[] { "Confirmed", "Cancelled" }, error.Details["allowedTransitions"]);
    }

    [Theory]
    [InlineData(OrderStatus.Fulfilled, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    public async Task Rejected_change_from_a_final_status_says_it_is_final(OrderStatus from, OrderStatus to)
    {
        var order = await SeedOrder(from);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _manager.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest { Status = to }));

        Assert.Equal($"Order PO-1001 is {from}, which is a final status, so it can't be changed to {to}.", error.Message);
        Assert.Equal(Array.Empty<string>(), error.Details["allowedTransitions"]);
    }

    [Fact]
    public async Task Unknown_order_throws_not_found()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            _manager.ChangeStatusAsync(id, new ChangeOrderStatusRequest { Status = OrderStatus.Confirmed }));

        Assert.Equal($"PurchaseOrder '{id}' was not found.", error.Message);
    }

    [Fact]
    public async Task Missing_status_throws_validation_exception()
    {
        var order = await SeedOrder(OrderStatus.Pending);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest()));
    }

    private async Task<PurchaseOrder> SeedOrder(OrderStatus status)
    {
        await _manager.SubmitAsync(TestData.ValidRequest());
        var order = _repository.Orders.Single();
        order.Status = status;
        return order;
    }
}
