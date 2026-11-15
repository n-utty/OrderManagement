namespace OrderManagement.Domain.PurchaseOrders.Models;

public sealed record CreatePurchaseOrderRequest
{
    public string ExternalReference { get; init; } = string.Empty;

    public string CustomerId { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string CustomerEmail { get; init; } = string.Empty;

    public string Currency { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public IReadOnlyList<LineItemRequest> LineItems { get; init; } = [];
}
