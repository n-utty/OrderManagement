using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public class PurchaseOrderRepository(AppDbContext context)
    : Repository<PurchaseOrder>(context), IPurchaseOrderRepository
{
    private const int SqliteConstraintUnique = 2067;

    public Task<PurchaseOrder?> GetWithLineItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(order => order.LineItems)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task<PurchaseOrder?> GetByReferenceAsync(string externalReference, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(order => order.LineItems)
            .FirstOrDefaultAsync(order => order.ExternalReference == externalReference, cancellationToken);

    public async Task<PagedResult<PurchaseOrder>> ListAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(order => order.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Id breaks ties between orders saved in the same tick, so pages never overlap or skip rows.
        var items = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(order => order.LineItems)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrder>(items, page, pageSize, totalCount);
    }

    public async Task<bool> TryAddAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(order, cancellationToken);

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsDuplicateReference(exception))
        {
            // The failed insert is still tracked as Added; clearing it leaves the context usable
            // for the follow-up lookup of the order that won the race.
            Context.ChangeTracker.Clear();
            return false;
        }
    }

    // Only the external reference index counts as "already exists"; any other constraint
    // failure is a genuine error and must surface.
    private static bool IsDuplicateReference(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique } sqlite
        && sqlite.Message.Contains(
            $"{nameof(AppDbContext.PurchaseOrders)}.{nameof(PurchaseOrder.ExternalReference)}",
            StringComparison.Ordinal);
}
