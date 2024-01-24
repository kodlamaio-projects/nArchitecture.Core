using Core.Persistence.DbMigrationApplier;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Persistence.DependencyInjection;

public static class ServiceCollectionDbMigrationApplierExtensions
{
    public static IServiceCollection AddDbMigrationApplier<TDbContext>(
        this IServiceCollection services,
        Func<ServiceProvider, TDbContext> contextFactory
    )
        where TDbContext : DbContext
    {
        ServiceProvider buildServiceProvider = services.BuildServiceProvider();

        _ = services.AddTransient<IDbMigrationApplierService, DbMigrationApplierManager<TDbContext>>(
            _ => new DbMigrationApplierManager<TDbContext>(contextFactory(buildServiceProvider))
        );
        _ = services.AddTransient<IDbMigrationApplierService<TDbContext>, DbMigrationApplierManager<TDbContext>>(
            _ => new DbMigrationApplierManager<TDbContext>(contextFactory(buildServiceProvider))
        );

        return services;
    }
}
