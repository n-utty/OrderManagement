using System.Collections.Immutable;

namespace OrderManagement.Domain.PurchaseOrders;

// One table is the single source of truth: the manager enforces it and the
// responses expose it, so the UI only ever offers moves the server will accept.
public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, ImmutableArray<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Fulfilled, OrderStatus.Cancelled],
        [OrderStatus.Fulfilled] = [],
        [OrderStatus.Cancelled] = [],
    };

    public static IReadOnlyList<OrderStatus> AllowedFrom(OrderStatus status) =>
        Allowed.TryGetValue(status, out var next) ? next : [];

    public static bool CanTransition(OrderStatus from, OrderStatus to) => AllowedFrom(from).Contains(to);

    public static bool IsFinal(OrderStatus status) => AllowedFrom(status).Count == 0;
}
