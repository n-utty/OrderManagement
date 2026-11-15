using FluentValidation;
using OrderManagement.Domain.Common.Exceptions;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.Tests.Fakes;

namespace OrderManagement.Domain.Tests.PurchaseOrders;

public class SubmitPurchaseOrderTests
{
    private readonly InMemoryPurchaseOrderRepository _repository = new();
    private readonly PurchaseOrderManager _manager;

    public SubmitPurchaseOrderTests() => _manager = TestData.CreateManager(_repository);

    [Fact]
    public async Task New_order_is_created_pending_with_server_side_totals()
    {
        var result = await _manager.SubmitAsync(TestData.ValidRequest());

        Assert.True(result.WasCreated);
        Assert.Equal(OrderStatus.Pending, result.Order.Status);
        Assert.Equal(TestData.ValidRequestTotal, result.Order.Subtotal);
        Assert.Equal(TestData.ValidRequestTotal, result.Order.Total);
        Assert.Equal([31.50m, 99.99m], result.Order.LineItems.Select(item => item.LineTotal));
        Assert.Equal([OrderStatus.Confirmed, OrderStatus.Cancelled], result.Order.AllowedTransitions);

        var stored = Assert.Single(_repository.Orders);
        Assert.Equal(result.Order.Id, stored.Id);
        Assert.Equal(PurchaseOrderFingerprint.Compute(stored), stored.Fingerprint);
        Assert.All(stored.LineItems, item => Assert.Equal(stored.Id, item.PurchaseOrderId));
    }

    [Fact]
    public async Task Input_is_normalised_before_it_is_stored()
    {
        var request = TestData.ValidRequest(" po-1001 ") with
        {
            CustomerName = "  Acme Trading ",
            Currency = "zar",
            Notes = "   ",
        };

        var result = await _manager.SubmitAsync(request);

        Assert.Equal("PO-1001", result.Order.ExternalReference);
        Assert.Equal("Acme Trading", result.Order.CustomerName);
        Assert.Equal("ZAR", result.Order.Currency);
        Assert.Null(result.Order.Notes);
    }

    [Fact]
    public async Task Resubmitting_the_same_order_returns_the_original_without_a_duplicate()
    {
        var first = await _manager.SubmitAsync(TestData.ValidRequest());

        var second = await _manager.SubmitAsync(TestData.ValidRequest());

        Assert.False(second.WasCreated);
        Assert.Equal(first.Order, second.Order with { LineItems = first.Order.LineItems, AllowedTransitions = first.Order.AllowedTransitions });
        Assert.Equal(first.Order.LineItems, second.Order.LineItems);
        Assert.Single(_repository.Orders);
    }

    [Fact]
    public async Task Retry_with_cosmetic_differences_is_still_a_replay()
    {
        var first = await _manager.SubmitAsync(TestData.ValidRequest());
        var original = TestData.ValidRequest();
        var retry = original with
        {
            ExternalReference = "  po-1001",
            CustomerEmail = "BUYER@acme.test",
            LineItems = [original.LineItems[1], original.LineItems[0] with { UnitPrice = 10.5m }],
        };

        var second = await _manager.SubmitAsync(retry);

        Assert.False(second.WasCreated);
        Assert.Equal(first.Order.Id, second.Order.Id);
        Assert.Single(_repository.Orders);
    }

    [Fact]
    public async Task Same_reference_with_different_content_is_a_conflict_pointing_at_the_existing_order()
    {
        var first = await _manager.SubmitAsync(TestData.ValidRequest());
        var original = TestData.ValidRequest();
        var changed = original with { LineItems = [original.LineItems[0] with { Quantity = 4 }, original.LineItems[1]] };

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => _manager.SubmitAsync(changed));

        Assert.Equal(first.Order.Id, conflict.ExistingOrderId);
        Assert.Contains("PO-1001", conflict.Message);
        Assert.Contains("already exists with different details", conflict.Message);
        Assert.Single(_repository.Orders);
    }

    [Fact]
    public async Task Different_references_create_separate_orders()
    {
        await _manager.SubmitAsync(TestData.ValidRequest("PO-1001"));
        var second = await _manager.SubmitAsync(TestData.ValidRequest("PO-1002"));

        Assert.True(second.WasCreated);
        Assert.Equal(2, _repository.Orders.Count);
    }

    [Fact]
    public async Task Invalid_request_throws_validation_exception_and_stores_nothing()
    {
        var request = TestData.ValidRequest() with { Currency = "JPY", LineItems = [] };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _manager.SubmitAsync(request));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreatePurchaseOrderRequest.Currency));
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreatePurchaseOrderRequest.LineItems));
        Assert.Empty(_repository.Orders);
    }

    [Fact]
    public async Task Losing_an_insert_race_to_an_identical_order_replays_the_winner()
    {
        var winner = PersistedCopyOf(TestData.ValidRequest());
        _repository.ConcurrentWinner = winner;

        var result = await _manager.SubmitAsync(TestData.ValidRequest());

        Assert.False(result.WasCreated);
        Assert.Equal(winner.Id, result.Order.Id);
        Assert.Single(_repository.Orders);
    }

    [Fact]
    public async Task Losing_an_insert_race_to_a_different_order_is_a_conflict()
    {
        var original = TestData.ValidRequest();
        var winner = PersistedCopyOf(original with { Notes = "Different notes" });
        _repository.ConcurrentWinner = winner;

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => _manager.SubmitAsync(original));

        Assert.Equal(winner.Id, conflict.ExistingOrderId);
        Assert.Single(_repository.Orders);
    }

    // Builds the order another request would have saved, by running it through a separate manager.
    private static PurchaseOrder PersistedCopyOf(CreatePurchaseOrderRequest request)
    {
        var otherRepository = new InMemoryPurchaseOrderRepository();
        TestData.CreateManager(otherRepository).SubmitAsync(request).GetAwaiter().GetResult();
        return otherRepository.Orders.Single();
    }
}
