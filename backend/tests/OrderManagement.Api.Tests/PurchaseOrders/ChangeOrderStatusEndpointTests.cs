using System.Net;
using System.Net.Http.Json;
using OrderManagement.Api.Tests.Support;

namespace OrderManagement.Api.Tests.PurchaseOrders;

public class ChangeOrderStatusEndpointTests(OrderManagementApiFactory factory) : IClassFixture<OrderManagementApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_valid_status_change_returns_200_with_the_new_status()
    {
        var id = await CreateOrderAsync();

        var response = await _client.PutAsJsonAsync($"{ApiRequests.PurchaseOrders}/{id}/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Confirmed", (await response.ReadJsonAsync()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task An_invalid_status_change_returns_422_with_the_allowed_transitions()
    {
        var id = await CreateOrderAsync();

        var response = await _client.PutAsJsonAsync($"{ApiRequests.PurchaseOrders}/{id}/status", new { status = "Fulfilled" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var allowed = (await response.ReadJsonAsync()).GetProperty("allowedTransitions")
            .EnumerateArray()
            .Select(status => status.GetString());
        Assert.Equal(["Confirmed", "Cancelled"], allowed);
    }

    private async Task<Guid> CreateOrderAsync()
    {
        var response = await _client.PostAsJsonAsync(ApiRequests.PurchaseOrders, ApiRequests.CreateOrder(ApiRequests.NewReference()));
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }
}
