using StuffTracker.Api.Common;
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Locations.UpdateLocation;

/// <summary>
/// Endpoint for updating a location's name.
/// PATCH /api/locations/{id}
/// </summary>
public class UpdateLocationEndpoint : BaseEndpoint<UpdateLocationRequest, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;

    public UpdateLocationEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Patch("/locations/{id}");
        Summary(s =>
        {
            s.Summary = "Update a location";
            s.Description = "Updates the name of an existing location.";
            s.Responses[200] = "Location updated successfully";
            s.Responses[400] = "Validation error";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Location not found";
        });
    }

    public override async Task HandleAsync(UpdateLocationRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        var location = await _locationRepository.GetByIdAsync(req.Id, userId, ct);
        if (location == null)
        {
            await SendNotFoundAsync("Location not found", ct);
            return;
        }

        location.Name = req.Name;

        var updated = await _locationRepository.UpdateAsync(location, ct);

        // Ensure BreadcrumbIds is populated (fallback for legacy locations)
        var breadcrumbIds = updated.PathIds ?? Array.Empty<Guid>();
        if (breadcrumbIds.Length == 0 && updated.PathNames.Length > 0)
        {
            breadcrumbIds = await _locationRepository.GetBreadcrumbIdsAsync(updated.Id, ct);
            if (breadcrumbIds.Length == 0)
            {
                breadcrumbIds = new[] { updated.Id };
            }
        }

        Response = new LocationResponse
        {
            Id = updated.Id,
            Name = updated.Name,
            ParentId = updated.ParentId,
            Breadcrumbs = updated.PathNames,
            BreadcrumbIds = breadcrumbIds,
            Depth = updated.Depth,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }
}
