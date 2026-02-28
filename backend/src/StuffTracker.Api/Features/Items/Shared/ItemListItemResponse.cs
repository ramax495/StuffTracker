namespace StuffTracker.Api.Features.Items.Shared;

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
