using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.PurchaseOrders;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetWithLineItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Finds an order by its normalised external reference, including its line items.</summary>
    Task<PurchaseOrder?> GetByReferenceAsync(string externalReference, CancellationToken cancellationToken = default);

    /// <summary>Lists orders newest first, including their line items.</summary>
    Task<PagedResult<PurchaseOrder>> ListAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts and saves the order. Returns false instead of throwing when another order already
    /// holds the same external reference: a concurrent retry can win the race between the
    /// lookup and the insert, and the caller then replays that order instead of failing.
    /// </summary>
    Task<bool> TryAddAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
}
