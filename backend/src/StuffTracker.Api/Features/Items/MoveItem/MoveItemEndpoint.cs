using StuffTracker.Api.Common;
using StuffTracker.Api.Features.Items.Shared;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Items.MoveItem;

/// <summary>
/// Endpoint for moving an item to a different location.
/// POST /api/items/{id}/move
/// </summary>
public class MoveItemEndpoint : BaseEndpoint<MoveItemRequest, ItemResponse>
{
    private readonly IItemRepository _itemRepository;
    private readonly ILocationRepository _locationRepository;

    public MoveItemEndpoint(IItemRepository itemRepository, ILocationRepository locationRepository)
    {
        _itemRepository = itemRepository;
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Post("/items/{id}/move");
        Summary(s =>
        {
            s.Summary = "Move an item to a new location";
            s.Description = "Moves an existing item to a different storage location.";
            s.Responses[200] = "Item moved successfully";
            s.Responses[400] = "Validation error";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Item or target location not found";
        });
    }

    public override async Task HandleAsync(MoveItemRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        // Get item by ID and verify ownership
        var item = await _itemRepository.GetByIdAsync(req.Id, userId, ct);
        if (item == null)
        {
            await SendNotFoundAsync("Item not found", ct);
            return;
        }

        // Verify target location exists and belongs to user
        var targetLocation = await _locationRepository.GetByIdAsync(req.LocationId, userId, ct);
        if (targetLocation == null)
        {
            await SendNotFoundAsync("Target location not found", ct);
            return;
        }

        // Update item's LocationId
        item.LocationId = req.LocationId;
        var updated = await _itemRepository.UpdateAsync(item, ct);

        Response = new ItemResponse
        {
            Id = updated.Id,
            Name = updated.Name,
            Description = updated.Description,
            Quantity = updated.Quantity,
            LocationId = updated.LocationId,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }
}
