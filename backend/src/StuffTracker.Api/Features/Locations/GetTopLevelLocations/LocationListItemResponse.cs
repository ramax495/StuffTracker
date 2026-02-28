namespace StuffTracker.Api.Features.Locations.GetTopLevelLocations;

/// <summary>
/// Response DTO for location list items.
/// Per api.yaml LocationListItem schema.
/// </summary>
public class LocationListItemResponse
{
    /// <summary>
    /// Location UUID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Location name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of direct child locations.
    /// </summary>
    public int ChildCount { get; set; }

    /// <summary>
    /// Number of items directly in this location.
    /// </summary>
    public int ItemCount { get; set; }
}
