namespace StuffTracker.Api.Features.Items.CreateItem;

/// <summary>
/// Request DTO for creating a new item.
/// Per api.yaml CreateItemRequest schema.
/// </summary>
public class CreateItemRequest
{
    /// <summary>
    /// Item name (1-200 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of items (optional, defaults to 1, must be >= 1).
    /// </summary>
    public int? Quantity { get; set; }

    /// <summary>
    /// Storage location ID (required).
    /// </summary>
    public Guid LocationId { get; set; }
}
