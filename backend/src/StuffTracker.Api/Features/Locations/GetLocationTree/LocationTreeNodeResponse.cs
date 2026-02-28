namespace StuffTracker.Api.Features.Locations.GetLocationTree;

/// <summary>
/// Response DTO for a location tree node.
/// Per api.yaml LocationTreeNode schema.
/// </summary>
public class LocationTreeNodeResponse
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
    /// Hierarchy depth (0 for top-level).
    /// </summary>
    public short Depth { get; set; }

    /// <summary>
    /// Child nodes.
    /// </summary>
    public List<LocationTreeNodeResponse> Children { get; set; } = new();
}
