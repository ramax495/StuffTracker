using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StuffTracker.Api.Infrastructure.Persistence;

/// <summary>
/// Factory for creating DbContext at design time (for EF Core migrations).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Use a placeholder connection string for design-time migration generation
        // The actual connection string will be provided at runtime
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=stufftracker;Username=stufftracker;Password=stufftracker_dev_password");

        return new AppDbContext(optionsBuilder.Options);
    }
}
