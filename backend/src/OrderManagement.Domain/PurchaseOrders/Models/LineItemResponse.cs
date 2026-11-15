namespace OrderManagement.Domain.PurchaseOrders.Models;

public sealed record LineItemResponse(string Sku, string Name, int Quantity, decimal UnitPrice, decimal LineTotal);
