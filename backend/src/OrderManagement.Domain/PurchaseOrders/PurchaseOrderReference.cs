namespace OrderManagement.Domain.PurchaseOrders;

// Shared by the validator and the manager so the value that is validated is
// exactly the value that is stored and looked up. Reps retype references by
// hand, so " po-1001" and "PO-1001" must count as the same order.
public static class PurchaseOrderReference
{
    public static string Normalise(string? reference) => (reference ?? string.Empty).Trim().ToUpperInvariant();
}
