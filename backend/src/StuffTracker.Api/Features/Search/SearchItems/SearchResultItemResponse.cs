namespace StuffTracker.Api.Features.Search.SearchItems;

/// <summary>
/// Represents a single item in search results.
/// </summary>
public class SearchResultItemResponse
{
    /// <summary>
    /// Unique identifier of the item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Quantity of the item.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// ID of the location where the item is stored.
    /// </summary>
    public Guid LocationId { get; set; }

    /// <summary>
    /// Full path to the location as breadcrumbs (e.g., ["House", "Kitchen", "Cabinet"]).
    /// </summary>
    public string[] LocationPath { get; set; } = Array.Empty<string>();
}
