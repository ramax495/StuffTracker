using Microsoft.AspNetCore.Http;
using StuffTracker.Api.Common;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Locations.DeleteLocation;

/// <summary>
/// Request for deleting a location.
/// </summary>
public class DeleteLocationRequest
{
    /// <summary>
    /// Location ID to delete.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Force delete even if location has contents (query param).
    /// </summary>
    public bool Force { get; set; }
}

/// <summary>
/// Endpoint for deleting a location.
/// DELETE /api/locations/{id}
/// </summary>
public class DeleteLocationEndpoint : BaseEndpointWithoutResponse<DeleteLocationRequest>
{
    private readonly ILocationRepository _locationRepository;

    public DeleteLocationEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Delete("/locations/{id}");
        Summary(s =>
        {
            s.Summary = "Delete a location";
            s.Description = "Deletes a location. If the location has children or items and force=false, returns 409 Conflict with details about contents.";
            s.Responses[204] = "Location deleted successfully";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Location not found";
            s.Responses[409] = "Location has contents (use force=true to delete anyway)";
        });
    }

    public override async Task HandleAsync(DeleteLocationRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        var location = await _locationRepository.GetByIdAsync(req.Id, userId, ct);
        if (location == null)
        {
            await SendNotFoundAsync("Location not found", ct);
            return;
        }

        if (!req.Force)
        {
            var counts = await _locationRepository.CountChildrenAndItemsAsync(req.Id, userId, ct);
            if (counts.ChildCount > 0 || counts.ItemCount > 0)
            {
                await SendLocationDeleteConflictAsync(counts.ChildCount, counts.ItemCount, counts.TotalDescendantItems, ct);
                return;
            }
        }

        await _locationRepository.DeleteAsync(req.Id, userId, force: true, ct);

        HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
