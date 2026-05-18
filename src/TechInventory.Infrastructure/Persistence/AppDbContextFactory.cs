using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TechInventory.Infrastructure.Persistence.Interceptors;
using TechInventory.Infrastructure.Services;

namespace TechInventory.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=techinventory.db");

        return new AppDbContext(
            optionsBuilder.Options,
            new AuditSaveChangesInterceptor(new SystemCurrentUserService()));
    }
}
