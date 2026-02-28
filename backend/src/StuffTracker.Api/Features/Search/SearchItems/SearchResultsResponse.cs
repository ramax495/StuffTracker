namespace StuffTracker.Api.Features.Search.SearchItems;

/// <summary>
/// Response model for search results with pagination info.
/// </summary>
public class SearchResultsResponse
{
    /// <summary>
    /// List of items matching the search criteria.
    /// </summary>
    public List<SearchResultItemResponse> Items { get; set; } = new();

    /// <summary>
    /// Total number of items matching the search criteria.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Indicates whether there are more results available.
    /// </summary>
    public bool HasMore { get; set; }
}
