namespace StuffTracker.Api.Domain;

/// <summary>
/// Represents a physical item stored in a location.
/// </summary>
public class Item
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of this item (FK to User.TelegramId).
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Storage location (FK to StorageLocation.Id).
    /// </summary>
    public Guid LocationId { get; set; }

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
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public StorageLocation Location { get; set; } = null!;
}
