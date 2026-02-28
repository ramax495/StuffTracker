namespace StuffTracker.Api.Features.Items.UpdateItem;

/// <summary>
/// Request DTO for updating an item.
/// Per api.yaml UpdateItemRequest schema.
/// </summary>
public class UpdateItemRequest
{
    /// <summary>
    /// Item ID (from route).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Item name (1-200 characters, optional).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional description of the item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of items (optional, must be >= 1 if provided).
    /// </summary>
    public int? Quantity { get; set; }
}
