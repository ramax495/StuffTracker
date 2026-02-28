using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for StorageLocation operations with CTE support for hierarchical queries.
/// </summary>
public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _dbContext;

    public LocationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<List<LocationListResult>> GetTopLevelLocationsAsync(long userId, CancellationToken ct = default)
    {
        return await _dbContext.StorageLocations
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.ParentId == null)
            .Select(l => new LocationListResult
            {
                Id = l.Id,
                Name = l.Name,
                ChildCount = l.Children.Count,
                ItemCount = l.Items.Count
            })
            .OrderBy(l => l.Name)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<StorageLocation?> GetByIdAsync(Guid id, long userId, CancellationToken ct = default)
    {
        return await _dbContext.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);
    }

    /// <inheritdoc />
    public async Task<List<LocationListResult>> GetChildrenAsync(Guid parentId, long userId, CancellationToken ct = default)
    {
        return await _dbContext.StorageLocations
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.ParentId == parentId)
            .Select(l => new LocationListResult
            {
                Id = l.Id,
                Name = l.Name,
                ChildCount = l.Children.Count,
                ItemCount = l.Items.Count
            })
            .OrderBy(l => l.Name)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<string[]> GetBreadcrumbsAsync(Guid id, CancellationToken ct = default)
    {
        var location = await _dbContext.StorageLocations
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => l.PathNames)
            .FirstOrDefaultAsync(ct);

        return location ?? Array.Empty<string>();
    }

    /// <inheritdoc />
    public async Task<Guid[]> GetBreadcrumbIdsAsync(Guid id, CancellationToken ct = default)
    {
        var location = await _dbContext.StorageLocations
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => l.PathIds)
            .FirstOrDefaultAsync(ct);

        return location ?? Array.Empty<Guid>();
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken ct = default)
    {
        // Check if we're using InMemory database (which doesn't support raw SQL)
        var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (isInMemory)
        {
            // Use recursive LINQ for InMemory database
            return await GetDescendantIdsRecursiveAsync(id, ct);
        }

        // Use raw SQL with CTE for PostgreSQL
        var sql = @"
            WITH RECURSIVE descendants AS (
                SELECT id FROM storage_locations WHERE id = {0}
                UNION ALL
                SELECT sl.id
                FROM storage_locations sl
                INNER JOIN descendants d ON sl.parent_id = d.id
            )
            SELECT id FROM descendants WHERE id != {0}";

        var descendantIds = await _dbContext.StorageLocations
            .FromSqlRaw(sql, id)
            .Select(l => l.Id)
            .ToListAsync(ct);

        return descendantIds;
    }

    /// <summary>
    /// Gets descendant IDs recursively using LINQ (for InMemory database).
    /// </summary>
    private async Task<List<Guid>> GetDescendantIdsRecursiveAsync(Guid parentId, CancellationToken ct)
    {
        var result = new List<Guid>();
        var children = await _dbContext.StorageLocations
            .AsNoTracking()
            .Where(l => l.ParentId == parentId)
            .Select(l => l.Id)
            .ToListAsync(ct);

        foreach (var childId in children)
        {
            result.Add(childId);
            var grandchildren = await GetDescendantIdsRecursiveAsync(childId, ct);
            result.AddRange(grandchildren);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<StorageLocation> CreateAsync(StorageLocation location, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        location.CreatedAt = now;
        location.UpdatedAt = now;

        // Calculate PathNames, PathIds, and Depth based on parent
        if (location.ParentId.HasValue)
        {
            var parent = await _dbContext.StorageLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == location.ParentId.Value, ct);

            if (parent != null)
            {
                location.PathNames = parent.PathNames.Append(location.Name).ToArray();
                location.PathIds = parent.PathIds.Append(location.Id).ToArray();
                location.Depth = (short)(parent.Depth + 1);
            }
            else
            {
                location.PathNames = new[] { location.Name };
                location.PathIds = new[] { location.Id };
                location.Depth = 0;
            }
        }
        else
        {
            location.PathNames = new[] { location.Name };
            location.PathIds = new[] { location.Id };
            location.Depth = 0;
        }

        _dbContext.StorageLocations.Add(location);
        await _dbContext.SaveChangesAsync(ct);

        return location;
    }

    /// <inheritdoc />
    public async Task<StorageLocation> UpdateAsync(StorageLocation location, CancellationToken ct = default)
    {
        location.UpdatedAt = DateTime.UtcNow;

        // Update PathNames for this location and all descendants if name changed
        var oldPathNames = await _dbContext.StorageLocations
            .Where(l => l.Id == location.Id)
            .Select(l => l.PathNames)
            .FirstOrDefaultAsync(ct);

        if (oldPathNames != null && oldPathNames.Length > 0)
        {
            var newPathNames = oldPathNames.Take(oldPathNames.Length - 1).Append(location.Name).ToArray();
            location.PathNames = newPathNames;

            // Update descendants' PathNames
            await UpdateDescendantPathNamesAsync(location.Id, oldPathNames, newPathNames, ct);
        }

        _dbContext.StorageLocations.Update(location);
        await _dbContext.SaveChangesAsync(ct);

        return location;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, long userId, bool force = false, CancellationToken ct = default)
    {
        var location = await _dbContext.StorageLocations
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

        if (location == null)
        {
            return false;
        }

        if (!force)
        {
            // Check for contents
            var hasChildren = await _dbContext.StorageLocations
                .AnyAsync(l => l.ParentId == id, ct);
            var hasItems = await _dbContext.Items
                .AnyAsync(i => i.LocationId == id, ct);

            if (hasChildren || hasItems)
            {
                return false;
            }
        }

        // With cascade delete configured in EF, removing the location will cascade
        _dbContext.StorageLocations.Remove(location);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<LocationTreeResult>> GetTreeAsync(long userId, CancellationToken ct = default)
    {
        return await _dbContext.StorageLocations
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => new LocationTreeResult
            {
                Id = l.Id,
                ParentId = l.ParentId,
                Name = l.Name,
                Depth = l.Depth
            })
            .OrderBy(l => l.Depth)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<LocationContentsCount> CountChildrenAndItemsAsync(Guid id, long userId, CancellationToken ct = default)
    {
        // Count direct children
        var childCount = await _dbContext.StorageLocations
            .CountAsync(l => l.ParentId == id && l.UserId == userId, ct);

        // Count direct items
        var itemCount = await _dbContext.Items
            .CountAsync(i => i.LocationId == id && i.UserId == userId, ct);

        // Count total descendant items using CTE
        var descendantIds = await GetDescendantIdsAsync(id, ct);
        var allLocationIds = descendantIds.Append(id).ToList();

        var totalDescendantItems = await _dbContext.Items
            .CountAsync(i => allLocationIds.Contains(i.LocationId) && i.UserId == userId, ct);

        return new LocationContentsCount
        {
            ChildCount = childCount,
            ItemCount = itemCount,
            TotalDescendantItems = totalDescendantItems
        };
    }

    /// <inheritdoc />
    public async Task<List<ItemListResult>> GetItemsAsync(Guid locationId, long userId, CancellationToken ct = default)
    {
        return await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.LocationId == locationId && i.UserId == userId)
            .Select(i => new ItemListResult
            {
                Id = i.Id,
                Name = i.Name,
                Quantity = i.Quantity
            })
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Updates PathNames for all descendants when a parent's name changes.
    /// </summary>
    private async Task UpdateDescendantPathNamesAsync(Guid parentId, string[] oldPathNames, string[] newPathNames, CancellationToken ct)
    {
        var descendants = await _dbContext.StorageLocations
            .Where(l => l.PathNames.Length > oldPathNames.Length)
            .ToListAsync(ct);

        foreach (var descendant in descendants)
        {
            // Check if this descendant is under the parent
            if (descendant.PathNames.Length >= oldPathNames.Length &&
                descendant.PathNames.Take(oldPathNames.Length).SequenceEqual(oldPathNames))
            {
                // Replace the old path prefix with the new one
                var suffix = descendant.PathNames.Skip(oldPathNames.Length).ToArray();
                descendant.PathNames = newPathNames.Concat(suffix).ToArray();
                descendant.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <inheritdoc />
    public async Task<StorageLocation?> MoveAsync(Guid id, Guid? newParentId, long userId, CancellationToken ct = default)
    {
        var location = await _dbContext.StorageLocations
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

        if (location == null)
        {
            return null;
        }

        // Store the old paths for descendants update
        var oldPathNames = location.PathNames;
        var oldPathIds = location.PathIds;
        var oldDepth = location.Depth;

        // Calculate new PathNames, PathIds, and Depth based on new parent
        string[] newParentPathNames;
        Guid[] newParentPathIds;
        short newParentDepth;

        if (newParentId.HasValue)
        {
            var newParent = await _dbContext.StorageLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == newParentId.Value, ct);

            if (newParent != null)
            {
                newParentPathNames = newParent.PathNames;
                newParentPathIds = newParent.PathIds;
                newParentDepth = newParent.Depth;
            }
            else
            {
                // Parent not found, treat as root
                newParentPathNames = Array.Empty<string>();
                newParentPathIds = Array.Empty<Guid>();
                newParentDepth = -1; // Will become 0 after +1
            }
        }
        else
        {
            // Moving to root
            newParentPathNames = Array.Empty<string>();
            newParentPathIds = Array.Empty<Guid>();
            newParentDepth = -1; // Will become 0 after +1
        }

        // Update the moved location
        location.ParentId = newParentId;
        location.Depth = (short)(newParentDepth + 1);
        location.PathNames = newParentPathNames.Append(location.Name).ToArray();
        location.PathIds = newParentPathIds.Append(location.Id).ToArray();
        location.UpdatedAt = DateTime.UtcNow;

        // Rebuild paths for all descendants
        await RebuildDescendantPathsAsync(location.Id, oldPathNames, location.PathNames, oldPathIds, location.PathIds, oldDepth, location.Depth, ct);

        await _dbContext.SaveChangesAsync(ct);

        return location;
    }

    /// <summary>
    /// Rebuilds PathNames, PathIds, and Depth for all descendants after a location is moved.
    /// </summary>
    private async Task RebuildDescendantPathsAsync(
        Guid movedLocationId,
        string[] oldPathNames,
        string[] newPathNames,
        Guid[] oldPathIds,
        Guid[] newPathIds,
        short oldDepth,
        short newDepth,
        CancellationToken ct)
    {
        // Get all descendants of the moved location
        var descendantIds = await GetDescendantIdsAsync(movedLocationId, ct);

        if (descendantIds.Count == 0)
        {
            return;
        }

        // Calculate the depth difference
        var depthDiff = newDepth - oldDepth;

        // Load all descendants
        var descendants = await _dbContext.StorageLocations
            .Where(l => descendantIds.Contains(l.Id))
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        foreach (var descendant in descendants)
        {
            // Update depth
            descendant.Depth = (short)(descendant.Depth + depthDiff);

            // Replace the old path prefix with the new one for PathNames
            // The descendant's path starts with oldPathNames, replace it with newPathNames
            if (descendant.PathNames.Length >= oldPathNames.Length &&
                descendant.PathNames.Take(oldPathNames.Length).SequenceEqual(oldPathNames))
            {
                var nameSuffix = descendant.PathNames.Skip(oldPathNames.Length).ToArray();
                descendant.PathNames = newPathNames.Concat(nameSuffix).ToArray();
            }

            // Replace the old path prefix with the new one for PathIds
            if (descendant.PathIds.Length >= oldPathIds.Length &&
                descendant.PathIds.Take(oldPathIds.Length).SequenceEqual(oldPathIds))
            {
                var idSuffix = descendant.PathIds.Skip(oldPathIds.Length).ToArray();
                descendant.PathIds = newPathIds.Concat(idSuffix).ToArray();
            }

            descendant.UpdatedAt = now;
        }
    }
}
