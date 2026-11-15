using OrderManagement.Domain.Common;
using OrderManagement.Domain.PurchaseOrders;

namespace OrderManagement.Domain.Tests.Fakes;

internal sealed class InMemoryPurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly List<PurchaseOrder> _orders = [];

    // A fixed, ticking clock keeps "newest first" deterministic in tests.
    private DateTime _clock = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    public IReadOnlyList<PurchaseOrder> Orders => _orders;

    public int SaveChangesCount { get; private set; }

    // Simulates another request saving this order between the manager's lookup and its insert.
    public PurchaseOrder? ConcurrentWinner { get; set; }

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_orders.FirstOrDefault(order => order.Id == id));

    public Task<PurchaseOrder?> GetWithLineItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<PurchaseOrder?> GetByReferenceAsync(string externalReference, CancellationToken cancellationToken = default) =>
        Task.FromResult(_orders.FirstOrDefault(order => order.ExternalReference == externalReference));

    public Task AddAsync(PurchaseOrder entity, CancellationToken cancellationToken = default)
    {
        Seed(entity);
        return Task.CompletedTask;
    }

    public void Update(PurchaseOrder entity) => entity.UpdatedAt = NextTimestamp();

    public void Remove(PurchaseOrder entity) => _orders.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;
        return Task.FromResult(1);
    }

    public Task<bool> TryAddAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        if (ConcurrentWinner is not null)
        {
            Seed(ConcurrentWinner);
            ConcurrentWinner = null;
        }

        if (_orders.Any(existing => existing.ExternalReference == order.ExternalReference))
        {
            return Task.FromResult(false);
        }

        Seed(order);
        SaveChangesCount++;
        return Task.FromResult(true);
    }

    public Task<PagedResult<PurchaseOrder>> ListAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var matching = _orders
            .Where(order => status is null || order.Status == status)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        var items = matching.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<PurchaseOrder>(items, page, pageSize, matching.Count));
    }

    public PurchaseOrder Seed(PurchaseOrder order)
    {
        order.CreatedAt = NextTimestamp();
        _orders.Add(order);
        return order;
    }

    private DateTime NextTimestamp() => _clock = _clock.AddMinutes(1);
}
