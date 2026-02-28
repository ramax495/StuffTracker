namespace StuffTracker.Api.Features.Items.Shared;

/// <summary>
/// Response DTO for a single item with location details.
/// Extends ItemResponse with locationPath and locationName.
/// Per api.yaml ItemDetailResponse schema.
/// </summary>
public class ItemDetailResponse : ItemResponse
{
    /// <summary>
    /// Breadcrumb path from root to the item's location.
    /// </summary>
    public string[] LocationPath { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Name of the location where the item is stored.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;
}
