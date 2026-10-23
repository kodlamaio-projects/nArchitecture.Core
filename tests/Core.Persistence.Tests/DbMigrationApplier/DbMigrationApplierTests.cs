using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Persistence.DbMigrationApplier;
using NArchitecture.Core.Persistence.DependencyInjection;
using NArchitecture.Core.Persistence.WebApi;

namespace Core.Persistence.Tests.DbMigrationApplier;

public class DbMigrationApplierTests
{
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

    private static ServiceProvider buildProvider()
    {
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddDbMigrationApplier(sp => sp.GetRequiredService<TestDbContext>());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public void AddDbMigrationApplier_RegistersBothServiceShapes()
    {
        using ServiceProvider provider = buildProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<DbMigrationApplierManager<TestDbContext>>(
            scope.ServiceProvider.GetRequiredService<IDbMigrationApplierService>()
        );
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDbMigrationApplierService<TestDbContext>>());
    }

    [Fact]
    public void UseDbMigrationApplier_ResolvesScopedDbContextWithScopeValidationEnabled()
    {
        using ServiceProvider provider = buildProvider();
        ApplicationBuilder app = new(provider);

        Exception? exception = Record.Exception(() => app.UseDbMigrationApplier());

        Assert.Null(exception);
    }
}
