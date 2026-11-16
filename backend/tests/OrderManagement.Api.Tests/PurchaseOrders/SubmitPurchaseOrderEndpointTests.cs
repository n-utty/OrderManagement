using System.Net;
using System.Net.Http.Json;
using OrderManagement.Api.Tests.Support;

namespace OrderManagement.Api.Tests.PurchaseOrders;

public class SubmitPurchaseOrderEndpointTests(OrderManagementApiFactory factory) : IClassFixture<OrderManagementApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Submitting_the_same_order_twice_returns_201_then_200_with_the_same_id()
    {
        var order = ApiRequests.CreateOrder(ApiRequests.NewReference());

        var created = await _client.PostAsJsonAsync(ApiRequests.PurchaseOrders, order);
        var replayed = await _client.PostAsJsonAsync(ApiRequests.PurchaseOrders, order);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replayed.StatusCode);

        var createdId = (await created.ReadJsonAsync()).GetProperty("id").GetGuid();
        var replayedId = (await replayed.ReadJsonAsync()).GetProperty("id").GetGuid();
        Assert.Equal(createdId, replayedId);

        Assert.NotNull(created.Headers.Location);
        Assert.EndsWith($"{ApiRequests.PurchaseOrders}/{createdId}", created.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reusing_a_reference_with_different_details_returns_409_with_the_existing_order_id()
    {
        var reference = ApiRequests.NewReference();
        var created = await _client.PostAsJsonAsync(ApiRequests.PurchaseOrders, ApiRequests.CreateOrder(reference));
        var existingId = (await created.ReadJsonAsync()).GetProperty("id").GetGuid();

        var conflict = await _client.PostAsJsonAsync(ApiRequests.PurchaseOrders, ApiRequests.CreateOrder(reference, widgetQuantity: 7));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
        Assert.Equal(existingId, (await conflict.ReadJsonAsync()).GetProperty("existingOrderId").GetGuid());
    }

    [Fact]
    public async Task An_invalid_order_returns_400_with_errors_grouped_by_field()
    {
        var invalid = new
        {
            externalReference = "",
            customerId = "CUST-001",
            customerName = "Acme Trading",
            customerEmail = "not-an-email",
            currency = "ZAR",
            lineItems = new[] { new { sku = "SKU-A", name = "Widget", quantity = 0, unitPrice = 10.50m } },
        };

        var response = await _client.PostAsJsonAsync(ApiRequests.PurchaseOrders, invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("ExternalReference", out _));
        Assert.True(errors.TryGetProperty("CustomerEmail", out _));
        Assert.True(errors.TryGetProperty("LineItems[0].Quantity", out _));
    }

    [Fact]
    public async Task A_unit_price_above_the_maximum_returns_400_instead_of_overflowing()
    {
        var order = new
        {
            externalReference = ApiRequests.NewReference(),
            customerId = "CUST-001",
            customerName = "Acme Trading",
            customerEmail = "orders@acme.example",
            currency = "ZAR",
            lineItems = new[] { new { sku = "SKU-A", name = "Widget", quantity = int.MaxValue, unitPrice = 1e25m } },
        };

        var response = await _client.PostAsJsonAsync(ApiRequests.PurchaseOrders, order);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("LineItems[0].UnitPrice", out _));
    }
}
