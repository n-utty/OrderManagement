namespace OrderManagement.Domain.PurchaseOrders.Models;

public sealed record ChangeOrderStatusRequest
{
    // Nullable so a missing status is rejected, rather than silently defaulting to Pending.
    public OrderStatus? Status { get; init; }
}
