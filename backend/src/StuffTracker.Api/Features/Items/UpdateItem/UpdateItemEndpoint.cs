using StuffTracker.Api.Common;
using StuffTracker.Api.Features.Items.Shared;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Items.UpdateItem;

/// <summary>
/// Endpoint for updating an item's properties.
/// PATCH /api/items/{id}
/// </summary>
public class UpdateItemEndpoint : BaseEndpoint<UpdateItemRequest, ItemResponse>
{
    private readonly IItemRepository _itemRepository;

    public UpdateItemEndpoint(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public override void Configure()
    {
        Patch("/items/{id}");
        Summary(s =>
        {
            s.Summary = "Update an item";
            s.Description = "Updates the name, description, and/or quantity of an existing item.";
            s.Responses[200] = "Item updated successfully";
            s.Responses[400] = "Validation error";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Item not found";
        });
    }

    public override async Task HandleAsync(UpdateItemRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        var item = await _itemRepository.GetByIdAsync(req.Id, userId, ct);
        if (item == null)
        {
            await SendNotFoundAsync("Item not found", ct);
            return;
        }

        // Apply updates only for provided fields
        if (req.Name != null)
        {
            item.Name = req.Name;
        }

        if (req.Description != null)
        {
            item.Description = req.Description;
        }

        if (req.Quantity.HasValue)
        {
            item.Quantity = req.Quantity.Value;
        }

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
