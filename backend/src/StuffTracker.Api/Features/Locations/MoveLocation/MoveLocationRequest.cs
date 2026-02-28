namespace StuffTracker.Api.Features.Locations.MoveLocation;

/// <summary>
/// Request DTO for moving a location to a new parent.
/// </summary>
public class MoveLocationRequest
{
    /// <summary>
    /// Location ID (from route).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// New parent location ID (null to move to root).
    /// </summary>
    public Guid? ParentId { get; set; }
}
