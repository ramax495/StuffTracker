using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new StorageLocationConfiguration());
        modelBuilder.ApplyConfiguration(new ItemConfiguration());
    }
}
