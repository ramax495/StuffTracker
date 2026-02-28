namespace StuffTracker.Api.Features.Locations.UpdateLocation;

/// <summary>
/// Request DTO for updating a location.
/// Per api.yaml UpdateLocationRequest schema.
/// </summary>
public class UpdateLocationRequest
{
    /// <summary>
    /// Location ID (from route).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// New location name (1-200 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
