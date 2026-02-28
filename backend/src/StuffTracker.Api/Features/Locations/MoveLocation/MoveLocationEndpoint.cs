using StuffTracker.Api.Common;
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Locations.MoveLocation;

/// <summary>
/// Endpoint for moving a location to a new parent.
/// POST /api/locations/{id}/move
/// Includes cycle detection to prevent invalid hierarchies.
/// </summary>
public class MoveLocationEndpoint : BaseEndpoint<MoveLocationRequest, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;

    public MoveLocationEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Post("/locations/{id}/move");
        Summary(s =>
        {
            s.Summary = "Move a location to a new parent";
            s.Description = "Moves an existing location to a different parent location or to root. Includes cycle detection to prevent moving a location into its own subtree.";
            s.Responses[200] = "Location moved successfully";
            s.Responses[400] = "Validation error or cycle detected";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Location or target parent not found";
        });
    }

    public override async Task HandleAsync(MoveLocationRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        // Get location by ID and verify ownership
        var location = await _locationRepository.GetByIdAsync(req.Id, userId, ct);
        if (location == null)
        {
            await SendNotFoundAsync("Location not found", ct);
            return;
        }

        // Cycle detection: Cannot move location to itself
        if (req.ParentId == location.Id)
        {
            await SendBadRequestAsync("Cannot move location to itself", ct);
            return;
        }

        // If moving to a specific parent, validate it
        if (req.ParentId.HasValue)
        {
            // Verify target parent exists and belongs to user
            var targetParent = await _locationRepository.GetByIdAsync(req.ParentId.Value, userId, ct);
            if (targetParent == null)
            {
                await SendNotFoundAsync("Target parent location not found", ct);
                return;
            }

            // Cycle detection: Cannot move location into its own descendants
            var descendantIds = await _locationRepository.GetDescendantIdsAsync(location.Id, ct);
            if (descendantIds.Contains(req.ParentId.Value))
            {
                await SendBadRequestAsync("Cannot move location into its own subtree", ct);
                return;
            }
        }

        // Skip if already at the target parent
        if (location.ParentId == req.ParentId)
        {
            // No change needed, return current state
            // Ensure BreadcrumbIds is populated (fallback for legacy locations)
            var breadcrumbIds = location.PathIds ?? Array.Empty<Guid>();
            if (breadcrumbIds.Length == 0 && location.PathNames.Length > 0)
            {
                breadcrumbIds = await _locationRepository.GetBreadcrumbIdsAsync(location.Id, ct);
                if (breadcrumbIds.Length == 0)
                {
                    breadcrumbIds = new[] { location.Id };
                }
            }

            Response = new LocationResponse
            {
                Id = location.Id,
                Name = location.Name,
                ParentId = location.ParentId,
                Breadcrumbs = location.PathNames,
                BreadcrumbIds = breadcrumbIds,
                Depth = location.Depth,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            };
            return;
        }

        // Perform the move with path rebuild
        var moved = await _locationRepository.MoveAsync(req.Id, req.ParentId, userId, ct);
        if (moved == null)
        {
            await SendNotFoundAsync("Location not found", ct);
            return;
        }

        // Ensure BreadcrumbIds is populated (fallback for legacy locations)
        var movedBreadcrumbIds = moved.PathIds ?? Array.Empty<Guid>();
        if (movedBreadcrumbIds.Length == 0 && moved.PathNames.Length > 0)
        {
            movedBreadcrumbIds = await _locationRepository.GetBreadcrumbIdsAsync(moved.Id, ct);
            if (movedBreadcrumbIds.Length == 0)
            {
                movedBreadcrumbIds = new[] { moved.Id };
            }
        }

        Response = new LocationResponse
        {
            Id = moved.Id,
            Name = moved.Name,
            ParentId = moved.ParentId,
            Breadcrumbs = moved.PathNames,
            BreadcrumbIds = movedBreadcrumbIds,
            Depth = moved.Depth,
            CreatedAt = moved.CreatedAt,
            UpdatedAt = moved.UpdatedAt
        };
    }
}
