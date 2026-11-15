using System.Net.Http.Json;
using System.Text.Json;

namespace OrderManagement.Api.Tests.Support;

internal static class ApiRequests
{
    public const string PurchaseOrders = "/api/purchase-orders";

    // Each test uses its own reference so tests sharing a database never collide.
    public static string NewReference() => $"PO-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    public static object CreateOrder(string reference, int widgetQuantity = 3) => new
    {
        externalReference = reference,
        customerId = "CUST-001",
        customerName = "Acme Trading",
        customerEmail = "buyer@acme.test",
        currency = "ZAR",
        lineItems = new[]
        {
            new { sku = "SKU-A", name = "Widget", quantity = widgetQuantity, unitPrice = 10.50m },
            new { sku = "SKU-B", name = "Gadget", quantity = 1, unitPrice = 99.99m },
        },
    };

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
