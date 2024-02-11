using Microsoft.EntityFrameworkCore;

namespace NArchitecture.Core.Persistence.DbMigrationApplier;

public class DbMigrationApplierManager<TDbContext> : IDbMigrationApplierService<TDbContext>
    where TDbContext : DbContext
{
    private readonly TDbContext _context;

    public DbMigrationApplierManager(TDbContext context)
    {
        _context = context;
    }

    public void Initialize()
    {
        _context.Database.EnsureDbApplied();
    }
}
