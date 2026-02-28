namespace StuffTracker.Api.Features.Items.MoveItem;

/// <summary>
/// Request DTO for moving an item to a new location.
/// Per api.yaml MoveItemRequest schema.
/// </summary>
public class MoveItemRequest
{
    /// <summary>
    /// Item ID (from route).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Target location ID (required).
    /// </summary>
    public Guid LocationId { get; set; }
}
