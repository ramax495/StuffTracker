using Microsoft.AspNetCore.Http;
using StuffTracker.Api.Common;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Items.DeleteItem;

/// <summary>
/// Request for deleting an item.
/// </summary>
public class DeleteItemRequest
{
    /// <summary>
    /// Item ID to delete.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Endpoint for deleting an item.
/// DELETE /api/items/{id}
/// </summary>
public class DeleteItemEndpoint : BaseEndpointWithoutResponse<DeleteItemRequest>
{
    private readonly IItemRepository _itemRepository;

    public DeleteItemEndpoint(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public override void Configure()
    {
        Delete("/items/{id}");
        Summary(s =>
        {
            s.Summary = "Delete an item";
            s.Description = "Deletes an item by ID.";
            s.Responses[204] = "Item deleted successfully";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Item not found";
        });
    }

    public override async Task HandleAsync(DeleteItemRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        var deleted = await _itemRepository.DeleteAsync(req.Id, userId, ct);
        if (!deleted)
        {
            await SendNotFoundAsync("Item not found", ct);
            return;
        }

        HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
