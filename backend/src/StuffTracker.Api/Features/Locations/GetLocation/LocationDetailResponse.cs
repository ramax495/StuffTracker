using StuffTracker.Api.Features.Locations.GetTopLevelLocations;
using StuffTracker.Api.Features.Locations.Shared;

namespace StuffTracker.Api.Features.Locations.GetLocation;

/// <summary>
/// Response DTO for a location with its children and items.
/// Per api.yaml LocationDetailResponse schema.
/// </summary>
public class LocationDetailResponse : LocationResponse
{
    /// <summary>
    /// Direct child locations.
    /// </summary>
    public List<LocationListItemResponse> Children { get; set; } = new();

    /// <summary>
    /// Items directly in this location.
    /// </summary>
    public List<ItemListItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Response DTO for item list items.
/// Per api.yaml ItemListItem schema.
/// </summary>
public class ItemListItemResponse
{
    /// <summary>
    /// Item UUID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Item quantity.
    /// </summary>
    public int Quantity { get; set; }
}
