namespace OrderManagement.Domain.PurchaseOrders.Models;

// WasCreated lets the API answer 201 for a new order and 200 for a replayed one,
// while the body is the same order either way.
public sealed record SubmitPurchaseOrderResult(PurchaseOrderResponse Order, bool WasCreated);
