namespace StuffTracker.Api.Domain;

/// <summary>
/// Represents a place where items can be stored.
/// Supports unlimited hierarchy depth through self-referencing ParentId.
/// </summary>
public class StorageLocation
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of this location (FK to User.TelegramId).
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Parent location (null for top-level locations).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Display name of the location.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Materialized breadcrumbs array for efficient path display.
    /// Example: ["Apartment", "Bedroom", "Closet", "Top Shelf"]
    /// </summary>
    public string[] PathNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Materialized breadcrumb IDs array for efficient navigation.
    /// Corresponds 1:1 with PathNames array.
    /// </summary>
    public Guid[] PathIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Hierarchy depth (0 = top-level).
    /// </summary>
    public short Depth { get; set; }

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
    public StorageLocation? Parent { get; set; }
    public ICollection<StorageLocation> Children { get; set; } = new List<StorageLocation>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
