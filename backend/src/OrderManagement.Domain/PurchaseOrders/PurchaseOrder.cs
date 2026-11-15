using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.PurchaseOrders;

public class PurchaseOrder : BaseEntity
{
    public string ExternalReference { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    // Hash of the order's content, compared on resubmission to tell an honest
    // retry apart from a different order reusing the same reference.
    public string Fingerprint { get; set; } = string.Empty;

    public List<LineItem> LineItems { get; set; } = [];

    public void CalculateTotals()
    {
        foreach (var item in LineItems)
        {
            item.LineTotal = item.Quantity * item.UnitPrice;
        }

        Subtotal = LineItems.Sum(item => item.LineTotal);

        // No tax, discounts or shipping yet. Total is kept separate from Subtotal
        // so those can be added later without changing the API shape.
        Total = Subtotal;
    }
}
