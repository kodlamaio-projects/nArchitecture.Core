using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Persistence.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.DependencyInjection;

public static class ServiceCollectionDbMigrationApplierExtensions
{
    /// <summary>
    /// Registers <see cref="IDbMigrationApplierService"/> and <see cref="IDbMigrationApplierService{TDbContext}"/>.
    /// The factory receives the request-scoped <see cref="IServiceProvider"/>; no intermediate container is built.
    /// </summary>
    public static IServiceCollection AddDbMigrationApplier<TDbContext>(
        this IServiceCollection services,
        Func<IServiceProvider, TDbContext> contextFactory
    )
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        _ = services.AddTransient<IDbMigrationApplierService, DbMigrationApplierManager<TDbContext>>(
            sp => new DbMigrationApplierManager<TDbContext>(contextFactory(sp))
        );
        _ = services.AddTransient<IDbMigrationApplierService<TDbContext>, DbMigrationApplierManager<TDbContext>>(
            sp => new DbMigrationApplierManager<TDbContext>(contextFactory(sp))
        );

        return services;
    }
}
