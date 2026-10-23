using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Persistence.DbMigrationApplier;

namespace NArchitecture.Core.Persistence.WebApi;

public static class ApplicationBuilderDbMigrationApplierExtensions
{
    /// <summary>
    /// Runs every registered <see cref="IDbMigrationApplierService"/> once inside a dedicated service scope,
    /// so scoped <c>DbContext</c> registrations resolve without tripping scope validation.
    /// </summary>
    public static IApplicationBuilder UseDbMigrationApplier(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using IServiceScope scope = app.ApplicationServices.CreateScope();
        IEnumerable<IDbMigrationApplierService> migrationCreatorServices =
            scope.ServiceProvider.GetServices<IDbMigrationApplierService>();
        foreach (IDbMigrationApplierService service in migrationCreatorServices)
            service.Initialize();

        return app;
    }
}
