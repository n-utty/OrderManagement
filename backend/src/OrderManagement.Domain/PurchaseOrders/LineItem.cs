namespace OrderManagement.Domain.PurchaseOrders;

public class LineItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PurchaseOrderId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
