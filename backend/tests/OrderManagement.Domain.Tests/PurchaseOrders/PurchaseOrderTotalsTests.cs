using OrderManagement.Domain.PurchaseOrders;

namespace OrderManagement.Domain.Tests.PurchaseOrders;

public class PurchaseOrderTotalsTests
{
    [Fact]
    public void Calculates_line_totals_subtotal_and_total()
    {
        var order = new PurchaseOrder
        {
            LineItems =
            [
                new LineItem { Sku = "A", Quantity = 3, UnitPrice = 10.50m },
                new LineItem { Sku = "B", Quantity = 1, UnitPrice = 99.99m },
                new LineItem { Sku = "C", Quantity = 5, UnitPrice = 0m },
            ],
        };

        order.CalculateTotals();

        Assert.Equal([31.50m, 99.99m, 0m], order.LineItems.Select(item => item.LineTotal));
        Assert.Equal(131.49m, order.Subtotal);
        Assert.Equal(order.Subtotal, order.Total);
    }

    [Fact]
    public void Uses_exact_decimal_arithmetic()
    {
        var order = new PurchaseOrder
        {
            LineItems =
            [
                new LineItem { Sku = "A", Quantity = 3, UnitPrice = 0.10m },
                new LineItem { Sku = "B", Quantity = 1_000_000, UnitPrice = 19.99m },
            ],
        };

        order.CalculateTotals();

        Assert.Equal(0.30m, order.LineItems[0].LineTotal);
        Assert.Equal(19_990_000.30m, order.Total);
    }

    [Fact]
    public void Recalculating_after_a_change_replaces_previous_totals()
    {
        var order = new PurchaseOrder { LineItems = [new LineItem { Sku = "A", Quantity = 2, UnitPrice = 5m }] };
        order.CalculateTotals();

        order.LineItems[0].Quantity = 4;
        order.CalculateTotals();

        Assert.Equal(20m, order.LineItems[0].LineTotal);
        Assert.Equal(20m, order.Total);
    }

    [Fact]
    public void Order_without_line_items_totals_zero()
    {
        var order = new PurchaseOrder();

        order.CalculateTotals();

        Assert.Equal(0m, order.Subtotal);
        Assert.Equal(0m, order.Total);
    }
}
