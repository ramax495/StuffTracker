using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for StorageLocation operations.
/// </summary>
public interface ILocationRepository
{
    /// <summary>
    /// Gets all top-level locations (ParentId is null) for a user.
    /// </summary>
    Task<List<LocationListResult>> GetTopLevelLocationsAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a single location by ID with ownership check.
    /// </summary>
    Task<StorageLocation?> GetByIdAsync(Guid id, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets direct children of a location.
    /// </summary>
    Task<List<LocationListResult>> GetChildrenAsync(Guid parentId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the breadcrumb path names for a location.
    /// </summary>
    Task<string[]> GetBreadcrumbsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the breadcrumb path IDs for a location (from root to current).
    /// </summary>
    Task<Guid[]> GetBreadcrumbIdsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all descendant IDs recursively using CTE.
    /// </summary>
    Task<List<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new location.
    /// </summary>
    Task<StorageLocation> CreateAsync(StorageLocation location, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing location.
    /// </summary>
    Task<StorageLocation> UpdateAsync(StorageLocation location, CancellationToken ct = default);

    /// <summary>
    /// Deletes a location (cascade deletes children and items if force is true).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, long userId, bool force = false, CancellationToken ct = default);

    /// <summary>
    /// Gets the full location tree for a user.
    /// </summary>
    Task<List<LocationTreeResult>> GetTreeAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Counts children and items for delete confirmation.
    /// </summary>
    Task<LocationContentsCount> CountChildrenAndItemsAsync(Guid id, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets items for a specific location.
    /// </summary>
    Task<List<ItemListResult>> GetItemsAsync(Guid locationId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Moves a location to a new parent and rebuilds paths for the location and all descendants.
    /// </summary>
    /// <param name="id">The location ID to move.</param>
    /// <param name="newParentId">The new parent ID (null to move to root).</param>
    /// <param name="userId">The user ID for ownership verification.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The moved location with updated path info, or null if not found.</returns>
    Task<StorageLocation?> MoveAsync(Guid id, Guid? newParentId, long userId, CancellationToken ct = default);
}

/// <summary>
/// Result DTO for location list items.
/// </summary>
public class LocationListResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ChildCount { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Result DTO for location tree nodes.
/// </summary>
public class LocationTreeResult
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public short Depth { get; set; }
}

/// <summary>
/// Result DTO for item list.
/// </summary>
public class ItemListResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>
/// Result DTO for counting location contents.
/// </summary>
public class LocationContentsCount
{
    public int ChildCount { get; set; }
    public int ItemCount { get; set; }
    public int TotalDescendantItems { get; set; }
}
