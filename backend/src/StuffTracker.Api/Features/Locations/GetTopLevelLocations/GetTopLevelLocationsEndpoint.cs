using StuffTracker.Api.Common;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Locations.GetTopLevelLocations;

/// <summary>
/// Endpoint for getting top-level locations (root locations without a parent).
/// GET /api/locations
/// </summary>
public class GetTopLevelLocationsEndpoint : BaseEndpointWithoutRequest<List<LocationListItemResponse>>
{
    private readonly ILocationRepository _locationRepository;

    public GetTopLevelLocationsEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Get("/locations");
        Summary(s =>
        {
            s.Summary = "Get top-level locations";
            s.Description = "Returns all root locations (locations without a parent) for the authenticated user.";
            s.Responses[200] = "Returns array of location list items";
            s.Responses[401] = "Invalid or missing Telegram authentication";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = GetUserId();

        var locations = await _locationRepository.GetTopLevelLocationsAsync(userId, ct);

        Response = locations.Select(l => new LocationListItemResponse
        {
            Id = l.Id,
            Name = l.Name,
            ChildCount = l.ChildCount,
            ItemCount = l.ItemCount
        }).ToList();
    }
}
