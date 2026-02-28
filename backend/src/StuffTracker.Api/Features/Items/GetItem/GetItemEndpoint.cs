using StuffTracker.Api.Common;
using StuffTracker.Api.Features.Items.Shared;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Items.GetItem;

/// <summary>
/// Request for getting a single item.
/// </summary>
public class GetItemRequest
{
    /// <summary>
    /// Item ID.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Endpoint for getting a single item with location details.
/// GET /api/items/{id}
/// </summary>
public class GetItemEndpoint : BaseEndpoint<GetItemRequest, ItemDetailResponse>
{
    private readonly IItemRepository _itemRepository;

    public GetItemEndpoint(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public override void Configure()
    {
        Get("/items/{id}");
        Summary(s =>
        {
            s.Summary = "Get an item by ID";
            s.Description = "Returns a single item with its location path (breadcrumbs) and location name.";
            s.Responses[200] = "Returns the item with location details";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Item not found";
        });
    }

    public override async Task HandleAsync(GetItemRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        var result = await _itemRepository.GetWithLocationPathAsync(req.Id, userId, ct);
        if (result == null)
        {
            await SendNotFoundAsync("Item not found", ct);
            return;
        }

        Response = new ItemDetailResponse
        {
            Id = result.Item.Id,
            Name = result.Item.Name,
            Description = result.Item.Description,
            Quantity = result.Item.Quantity,
            LocationId = result.Item.LocationId,
            CreatedAt = result.Item.CreatedAt,
            UpdatedAt = result.Item.UpdatedAt,
            LocationPath = result.LocationPath,
            LocationName = result.LocationName
        };
    }
}
