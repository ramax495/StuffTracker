using StuffTracker.Api.Common;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Search.SearchItems;

/// <summary>
/// Endpoint for searching items.
/// GET /api/search/items
/// </summary>
public class SearchItemsEndpoint : BaseEndpoint<SearchItemsRequest, SearchResultsResponse>
{
    private readonly IItemRepository _itemRepository;
    private readonly ILocationRepository _locationRepository;

    public SearchItemsEndpoint(IItemRepository itemRepository, ILocationRepository locationRepository)
    {
        _itemRepository = itemRepository;
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Get("/search/items");
        Summary(s =>
        {
            s.Summary = "Search items";
            s.Description = "Search for items by name with optional location filter. " +
                "Supports case-insensitive partial matching using PostgreSQL pg_trgm. " +
                "Empty query returns all items. Location filter includes all nested locations.";
            s.Responses[200] = "Search results returned successfully";
            s.Responses[400] = "Validation error";
            s.Responses[401] = "Invalid or missing Telegram authentication";
        });
    }

    public override async Task HandleAsync(SearchItemsRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        // Build location filter list including nested locations
        List<Guid>? locationIds = null;
        if (req.LocationId.HasValue)
        {
            // Verify the location exists and belongs to the user
            var location = await _locationRepository.GetByIdAsync(req.LocationId.Value, userId, ct);
            if (location == null)
            {
                await SendNotFoundAsync("Location not found", ct);
                return;
            }

            // Get all descendant location IDs using recursive CTE
            var descendantIds = await _locationRepository.GetDescendantIdsAsync(req.LocationId.Value, ct);

            // Include the root location and all descendants
            locationIds = new List<Guid> { req.LocationId.Value };
            locationIds.AddRange(descendantIds);
        }

        // Perform search
        var searchResult = await _itemRepository.SearchAsync(
            userId,
            req.Query,
            locationIds,
            req.Limit,
            req.Offset,
            ct);

        // Map to response
        Response = new SearchResultsResponse
        {
            Items = searchResult.Items.Select(i => new SearchResultItemResponse
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Quantity = i.Quantity,
                LocationId = i.LocationId,
                LocationPath = i.LocationPath
            }).ToList(),
            Total = searchResult.Total,
            HasMore = req.Offset + searchResult.Items.Count < searchResult.Total
        };
    }
}
