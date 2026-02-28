namespace StuffTracker.Api.Features.Locations.CreateLocation;

/// <summary>
/// Request DTO for creating a new location.
/// Per api.yaml CreateLocationRequest schema.
/// </summary>
public class CreateLocationRequest
{
    /// <summary>
    /// Location name (1-200 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent location ID (optional, null for top-level locations).
    /// </summary>
    public Guid? ParentId { get; set; }
}
