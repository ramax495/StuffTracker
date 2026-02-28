namespace StuffTracker.Api.Features.Locations.Shared;

/// <summary>
/// Response DTO for a single location.
/// Per api.yaml LocationResponse schema.
/// </summary>
public class LocationResponse
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
    /// Parent location ID (null for top-level locations).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Breadcrumb path from root to this location (names).
    /// </summary>
    public string[] Breadcrumbs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Breadcrumb path from root to this location (IDs).
    /// Corresponds 1:1 with the Breadcrumbs array.
    /// </summary>
    public Guid[] BreadcrumbIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Hierarchy depth (0 for top-level).
    /// </summary>
    public short Depth { get; set; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
