namespace StuffTracker.Api.Features.Items.Shared;

/// <summary>
/// Response DTO for a single item.
/// Per api.yaml ItemResponse schema.
/// </summary>
public class ItemResponse
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
    /// Optional description of the item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of items (default 1, must be positive).
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Storage location ID.
    /// </summary>
    public Guid LocationId { get; set; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
