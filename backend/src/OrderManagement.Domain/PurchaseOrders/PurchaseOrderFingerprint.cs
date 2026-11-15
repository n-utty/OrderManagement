using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OrderManagement.Domain.PurchaseOrders;

public static class PurchaseOrderFingerprint
{
    // The external reference is left out on purpose: it is the lookup key, and the
    // fingerprint answers a different question, "is this the same order content?".
    public static string Compute(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var canonical = new
        {
            CustomerId = order.CustomerId.Trim(),
            CustomerName = order.CustomerName.Trim(),
            CustomerEmail = order.CustomerEmail.Trim().ToLowerInvariant(),
            Currency = order.Currency.Trim().ToUpperInvariant(),
            Notes = order.Notes?.Trim() ?? string.Empty,
            LineItems = order.LineItems
                .Select(item => new
                {
                    Sku = item.Sku.Trim().ToUpperInvariant(),
                    Name = item.Name.Trim(),
                    item.Quantity,
                    UnitPrice = FormatPrice(item.UnitPrice),
                })
                // Line order is presentation only; a retry that lists items differently is still the same order.
                .OrderBy(item => item.Sku, StringComparer.Ordinal)
                .ToArray(),
        };

        var json = JsonSerializer.Serialize(canonical);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    // decimal keeps its scale (10.5 vs 10.50), so trailing zeros are stripped
    // to stop equal prices from producing different hashes.
    private static string FormatPrice(decimal price) =>
        price.ToString("0.############################", CultureInfo.InvariantCulture);
}
