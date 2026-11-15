using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.PurchaseOrders.Validators;
using OrderManagement.Domain.Tests.Fakes;

namespace OrderManagement.Domain.Tests;

internal static class TestData
{
    // 3 x 10.50 + 1 x 99.99
    public const decimal ValidRequestTotal = 131.49m;

    public static CreatePurchaseOrderRequest ValidRequest(string reference = "PO-1001") => new()
    {
        ExternalReference = reference,
        CustomerId = "CUST-001",
        CustomerName = "Acme Trading",
        CustomerEmail = "buyer@acme.test",
        Currency = "ZAR",
        Notes = "Deliver to loading bay 2",
        LineItems =
        [
            new LineItemRequest { Sku = "SKU-A", Name = "Widget", Quantity = 3, UnitPrice = 10.50m },
            new LineItemRequest { Sku = "SKU-B", Name = "Gadget", Quantity = 1, UnitPrice = 99.99m },
        ],
    };

    public static PurchaseOrderManager CreateManager(InMemoryPurchaseOrderRepository repository) =>
        new(repository, new CreatePurchaseOrderRequestValidator(), new ChangeOrderStatusRequestValidator());
}
