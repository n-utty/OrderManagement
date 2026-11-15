namespace OrderManagement.Domain.PurchaseOrders.Models;

// No line total here: totals are always calculated on the server.
public sealed record LineItemRequest
{
    public string Sku { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}
