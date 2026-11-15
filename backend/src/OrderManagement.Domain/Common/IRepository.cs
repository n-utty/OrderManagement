namespace OrderManagement.Domain.Common;

// Deliberately small: no IQueryable or GetAll, so query shapes stay in the
// specific repositories where they can be indexed and paged properly.
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
