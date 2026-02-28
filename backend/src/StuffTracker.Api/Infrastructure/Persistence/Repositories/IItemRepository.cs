using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for Item operations.
/// </summary>
public interface IItemRepository
{
    /// <summary>
    /// Gets a single item by ID with ownership check.
    /// </summary>
    Task<Item?> GetByIdAsync(Guid id, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all items in a specific location for a user.
    /// </summary>
    Task<List<Item>> GetByLocationIdAsync(Guid locationId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets an item with its location path (breadcrumbs).
    /// </summary>
    Task<ItemWithLocationPath?> GetWithLocationPathAsync(Guid id, long userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new item.
    /// </summary>
    Task<Item> CreateAsync(Item item, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    Task<Item> UpdateAsync(Item item, CancellationToken ct = default);

    /// <summary>
    /// Deletes an item.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, long userId, CancellationToken ct = default);

    /// <summary>
    /// Searches items by name with optional location filter.
    /// </summary>
    /// <param name="userId">User ID for ownership check.</param>
    /// <param name="query">Optional search query for item name (case-insensitive).</param>
    /// <param name="locationIds">Optional list of location IDs to filter by (includes nested).</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="offset">Number of results to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search result with items and total count.</returns>
    Task<SearchResult> SearchAsync(
        long userId,
        string? query,
        List<Guid>? locationIds,
        int limit,
        int offset,
        CancellationToken ct = default);
}

/// <summary>
/// Result DTO for search operation.
/// </summary>
public class SearchResult
{
    public List<SearchResultItem> Items { get; set; } = new();
    public int Total { get; set; }
}

/// <summary>
/// Result DTO for a single search result item.
/// </summary>
public class SearchResultItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public Guid LocationId { get; set; }
    public string[] LocationPath { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Result DTO for item with location path.
/// </summary>
public class ItemWithLocationPath
{
    public Item Item { get; set; } = null!;
    public string[] LocationPath { get; set; } = Array.Empty<string>();
    public string LocationName { get; set; } = string.Empty;
}
