using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Item operations.
/// </summary>
public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _dbContext;

    public ItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Item?> GetByIdAsync(Guid id, long userId, CancellationToken ct = default)
    {
        return await _dbContext.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);
    }

    /// <inheritdoc />
    public async Task<List<Item>> GetByLocationIdAsync(Guid locationId, long userId, CancellationToken ct = default)
    {
        return await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.LocationId == locationId && i.UserId == userId)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<ItemWithLocationPath?> GetWithLocationPathAsync(Guid id, long userId, CancellationToken ct = default)
    {
        // Use projection to avoid loading full entities and eliminate N+1
        return await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == id && i.UserId == userId)
            .Select(i => new ItemWithLocationPath
            {
                Item = i,
                LocationPath = i.Location.PathNames,
                LocationName = i.Location.Name
            })
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Item> CreateAsync(Item item, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        item.CreatedAt = now;
        item.UpdatedAt = now;

        _dbContext.Items.Add(item);
        await _dbContext.SaveChangesAsync(ct);

        return item;
    }

    /// <inheritdoc />
    public async Task<Item> UpdateAsync(Item item, CancellationToken ct = default)
    {
        item.UpdatedAt = DateTime.UtcNow;

        _dbContext.Items.Update(item);
        await _dbContext.SaveChangesAsync(ct);

        return item;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, long userId, CancellationToken ct = default)
    {
        var item = await _dbContext.Items
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);

        if (item == null)
        {
            return false;
        }

        _dbContext.Items.Remove(item);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(
        long userId,
        string? query,
        List<Guid>? locationIds,
        int limit,
        int offset,
        CancellationToken ct = default)
    {
        // Check if we're using InMemory database
        var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        // Build base query with AsNoTracking for read-only operation
        IQueryable<Item> baseQuery = _dbContext.Items
            .AsNoTracking()
            .Include(i => i.Location)
            .Where(i => i.UserId == userId);

        // Apply location filter if provided
        if (locationIds != null && locationIds.Count > 0)
        {
            baseQuery = baseQuery.Where(i => locationIds.Contains(i.LocationId));
        }

        // Apply search filter using ILIKE for case-insensitive search
        if (!string.IsNullOrWhiteSpace(query))
        {
            if (isInMemory)
            {
                // For InMemory database, we need to materialize the query first
                // then filter in memory because InMemory doesn't support string functions well
                var allItems = await baseQuery.ToListAsync(ct);
                var lowerQuery = query.ToLower();
                var filteredItems = allItems
                    .Where(i => i.Name.ToLower().Contains(lowerQuery))
                    .ToList();

                var total = filteredItems.Count;
                var pagedItems = filteredItems
                    .OrderBy(i => i.Name)
                    .ThenBy(i => i.Id)
                    .Skip(offset)
                    .Take(limit)
                    .Select(i => new SearchResultItem
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Description = i.Description,
                        Quantity = i.Quantity,
                        LocationId = i.LocationId,
                        LocationPath = i.Location.PathNames
                    })
                    .ToList();

                return new SearchResult
                {
                    Items = pagedItems,
                    Total = total
                };
            }
            else
            {
                // Use PostgreSQL ILIKE for case-insensitive search
                baseQuery = baseQuery.Where(i => EF.Functions.ILike(i.Name, $"%{query}%"));
            }
        }

        // Get total count before pagination
        var totalCount = await baseQuery.CountAsync(ct);

        // Apply pagination and ordering
        var items = await baseQuery
            .OrderBy(i => i.Name)
            .ThenBy(i => i.Id)
            .Skip(offset)
            .Take(limit)
            .Select(i => new SearchResultItem
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Quantity = i.Quantity,
                LocationId = i.LocationId,
                LocationPath = i.Location.PathNames
            })
            .ToListAsync(ct);

        return new SearchResult
        {
            Items = items,
            Total = totalCount
        };
    }
}
