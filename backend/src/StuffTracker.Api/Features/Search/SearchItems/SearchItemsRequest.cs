using FastEndpoints;

namespace StuffTracker.Api.Features.Search.SearchItems;

/// <summary>
/// Request model for searching items.
/// </summary>
public class SearchItemsRequest
{
    /// <summary>
    /// Optional search query for item name (1-100 characters).
    /// </summary>
    [QueryParam, BindFrom("q")]
    public string? Query { get; set; }

    /// <summary>
    /// Optional location ID to filter items (includes nested locations).
    /// </summary>
    [QueryParam]
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Maximum number of results to return (1-100, default 50).
    /// </summary>
    [QueryParam]
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Number of results to skip for pagination (default 0).
    /// </summary>
    [QueryParam]
    public int Offset { get; set; } = 0;
}
