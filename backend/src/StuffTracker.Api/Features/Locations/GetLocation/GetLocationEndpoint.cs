using StuffTracker.Api.Common;
using StuffTracker.Api.Features.Locations.GetTopLevelLocations;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Locations.GetLocation;

/// <summary>
/// Request for getting a single location.
/// </summary>
public class GetLocationRequest
{
    /// <summary>
    /// Location ID.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Endpoint for getting a single location with its children and items.
/// GET /api/locations/{id}
/// </summary>
public class GetLocationEndpoint : BaseEndpoint<GetLocationRequest, LocationDetailResponse>
{
    private readonly ILocationRepository _locationRepository;

    public GetLocationEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Get("/locations/{id}");
        Summary(s =>
        {
            s.Summary = "Get a location by ID";
            s.Description = "Returns a single location with its direct children and items.";
            s.Responses[200] = "Returns the location with children and items";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Location not found";
        });
    }

    public override async Task HandleAsync(GetLocationRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        var location = await _locationRepository.GetByIdAsync(req.Id, userId, ct);
        if (location == null)
        {
            await SendNotFoundAsync("Location not found", ct);
            return;
        }

        var children = await _locationRepository.GetChildrenAsync(req.Id, userId, ct);
        var items = await _locationRepository.GetItemsAsync(req.Id, userId, ct);

        // Ensure PathIds is populated (fallback for legacy locations created before PathIds migration)
        var breadcrumbIds = location.PathIds ?? Array.Empty<Guid>();
        if (breadcrumbIds.Length == 0 && location.PathNames.Length > 0)
        {
            // PathIds not populated - fetch from repository or rebuild
            breadcrumbIds = await _locationRepository.GetBreadcrumbIdsAsync(location.Id, ct);
            // If still empty and we have breadcrumbs, at minimum include the current location's ID
            if (breadcrumbIds.Length == 0)
            {
                breadcrumbIds = new[] { location.Id };
            }
        }

        Response = new LocationDetailResponse
        {
            Id = location.Id,
            Name = location.Name,
            ParentId = location.ParentId,
            Breadcrumbs = location.PathNames,
            BreadcrumbIds = breadcrumbIds,
            Depth = location.Depth,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt,
            Children = children.Select(c => new LocationListItemResponse
            {
                Id = c.Id,
                Name = c.Name,
                ChildCount = c.ChildCount,
                ItemCount = c.ItemCount
            }).ToList(),
            Items = items.Select(i => new ItemListItemResponse
            {
                Id = i.Id,
                Name = i.Name,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
