using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Infrastructure.Persistence.Converters;

namespace OrderManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<LineItem> LineItems => Set<LineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    // SQLite has no timezone-aware type, so every DateTime is normalised to UTC on the way in
    // and marked as UTC on the way out; otherwise reads come back as DateTimeKind.Unspecified.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
