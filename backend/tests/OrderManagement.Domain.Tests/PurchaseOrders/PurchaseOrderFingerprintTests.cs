using OrderManagement.Domain.PurchaseOrders;

namespace OrderManagement.Domain.Tests.PurchaseOrders;

public class PurchaseOrderFingerprintTests
{
    private static PurchaseOrder Order(params LineItem[] items) => new()
    {
        ExternalReference = "PO-1001",
        CustomerId = "CUST-001",
        CustomerName = "Acme Trading",
        CustomerEmail = "buyer@acme.test",
        Currency = "ZAR",
        Notes = "Deliver to bay 2",
        LineItems = items.ToList(),
    };

    private static LineItem Item(string sku, int quantity, decimal price) =>
        new() { Sku = sku, Name = $"Item {sku}", Quantity = quantity, UnitPrice = price };

    private static readonly PurchaseOrder Baseline = Order(Item("SKU-A", 3, 10.50m), Item("SKU-B", 1, 99.99m));

    [Fact]
    public void Is_a_sha256_hex_string()
    {
        var fingerprint = PurchaseOrderFingerprint.Compute(Baseline);

        Assert.Matches("^[0-9A-F]{64}$", fingerprint);
    }

    [Fact]
    public void Same_content_gives_same_fingerprint()
    {
        var retry = Order(Item("SKU-A", 3, 10.50m), Item("SKU-B", 1, 99.99m));

        Assert.Equal(PurchaseOrderFingerprint.Compute(Baseline), PurchaseOrderFingerprint.Compute(retry));
    }

    [Fact]
    public void Line_item_order_does_not_matter()
    {
        var reordered = Order(Item("SKU-B", 1, 99.99m), Item("SKU-A", 3, 10.50m));

        Assert.Equal(PurchaseOrderFingerprint.Compute(Baseline), PurchaseOrderFingerprint.Compute(reordered));
    }

    [Fact]
    public void Trailing_zeros_in_prices_do_not_matter()
    {
        var rescaled = Order(Item("SKU-A", 3, 10.5m), Item("SKU-B", 1, 99.990m));

        Assert.Equal(PurchaseOrderFingerprint.Compute(Baseline), PurchaseOrderFingerprint.Compute(rescaled));
    }

    [Fact]
    public void Casing_and_whitespace_in_sku_email_and_currency_do_not_matter()
    {
        var messy = Order(Item("SKU-A", 3, 10.50m), Item("SKU-B", 1, 99.99m));
        messy.LineItems[0].Sku = " sku-a ";
        messy.CustomerEmail = " Buyer@ACME.test ";
        messy.Currency = "zar";

        Assert.Equal(PurchaseOrderFingerprint.Compute(Baseline), PurchaseOrderFingerprint.Compute(messy));
    }

    [Fact]
    public void External_reference_is_not_part_of_the_fingerprint()
    {
        var other = Order(Item("SKU-A", 3, 10.50m), Item("SKU-B", 1, 99.99m));
        other.ExternalReference = "PO-9999";

        Assert.Equal(PurchaseOrderFingerprint.Compute(Baseline), PurchaseOrderFingerprint.Compute(other));
    }

    public static TheoryData<string, Action<PurchaseOrder>> Changes => new()
    {
        { "quantity", order => order.LineItems[0].Quantity = 4 },
        { "unit price", order => order.LineItems[0].UnitPrice = 10.51m },
        { "sku", order => order.LineItems[0].Sku = "SKU-C" },
        { "item name", order => order.LineItems[0].Name = "Renamed" },
        { "extra item", order => order.LineItems.Add(Item("SKU-C", 1, 1m)) },
        { "removed item", order => order.LineItems.RemoveAt(1) },
        { "customer id", order => order.CustomerId = "CUST-002" },
        { "customer name", order => order.CustomerName = "Other Co" },
        { "customer email", order => order.CustomerEmail = "other@acme.test" },
        { "currency", order => order.Currency = "USD" },
        { "notes", order => order.Notes = "Leave at reception" },
    };

    [Theory]
    [MemberData(nameof(Changes))]
    public void Any_content_change_gives_a_different_fingerprint(string _, Action<PurchaseOrder> change)
    {
        var changed = Order(Item("SKU-A", 3, 10.50m), Item("SKU-B", 1, 99.99m));
        change(changed);

        Assert.NotEqual(PurchaseOrderFingerprint.Compute(Baseline), PurchaseOrderFingerprint.Compute(changed));
    }
}
