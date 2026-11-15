using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;

namespace OrderManagement.Infrastructure.Tests.Support;

internal static class TestOrders
{
    public static PurchaseOrder Order(string reference, OrderStatus status = OrderStatus.Pending)
    {
        var order = new PurchaseOrder
        {
            ExternalReference = reference,
            CustomerId = "CUST-001",
            CustomerName = "Acme Trading",
            CustomerEmail = "buyer@acme.test",
            Currency = "ZAR",
            Status = status,
            Fingerprint = "FINGERPRINT",
        };

        order.LineItems.Add(new LineItem
        {
            PurchaseOrderId = order.Id,
            Sku = "SKU-A",
            Name = "Widget",
            Quantity = 2,
            UnitPrice = 10.50m,
        });

        order.CalculateTotals();
        return order;
    }

    public static CreatePurchaseOrderRequest Request(string reference) => new()
    {
        ExternalReference = reference,
        CustomerId = "CUST-001",
        CustomerName = "Acme Trading",
        CustomerEmail = "buyer@acme.test",
        Currency = "ZAR",
        LineItems =
        [
            new LineItemRequest { Sku = "SKU-A", Name = "Widget", Quantity = 3, UnitPrice = 10.50m },
            new LineItemRequest { Sku = "SKU-B", Name = "Gadget", Quantity = 1, UnitPrice = 99.99m },
        ],
    };
}
