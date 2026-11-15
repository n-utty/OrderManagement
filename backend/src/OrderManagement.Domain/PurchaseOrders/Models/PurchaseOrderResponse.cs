namespace OrderManagement.Domain.PurchaseOrders.Models;

public sealed record PurchaseOrderResponse(
    Guid Id,
    string ExternalReference,
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    string Currency,
    string? Notes,
    OrderStatus Status,
    decimal Subtotal,
    decimal Total,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<LineItemResponse> LineItems,
    IReadOnlyList<OrderStatus> AllowedTransitions);
