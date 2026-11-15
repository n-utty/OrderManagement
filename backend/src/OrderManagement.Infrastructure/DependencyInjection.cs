using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Interceptors;
using OrderManagement.Infrastructure.Repositories;

namespace OrderManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TimestampInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseSqlite(connectionString)
            .AddInterceptors(provider.GetRequiredService<TimestampInterceptor>()));

        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
