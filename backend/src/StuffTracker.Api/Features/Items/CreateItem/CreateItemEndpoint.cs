using Microsoft.AspNetCore.Http;
using StuffTracker.Api.Common;
using StuffTracker.Api.Domain;
using StuffTracker.Api.Features.Items.Shared;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Items.CreateItem;

/// <summary>
/// Endpoint for creating a new item.
/// POST /api/items
/// </summary>
public class CreateItemEndpoint : BaseEndpoint<CreateItemRequest, ItemResponse>
{
    private readonly IItemRepository _itemRepository;
    private readonly ILocationRepository _locationRepository;

    public CreateItemEndpoint(IItemRepository itemRepository, ILocationRepository locationRepository)
    {
        _itemRepository = itemRepository;
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Post("/items");
        Summary(s =>
        {
            s.Summary = "Create a new item";
            s.Description = "Creates a new item in the specified storage location for the authenticated user.";
            s.Responses[201] = "Item created successfully";
            s.Responses[400] = "Validation error";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Location not found";
        });
    }

    public override async Task HandleAsync(CreateItemRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        // Validate location exists and belongs to user
        var location = await _locationRepository.GetByIdAsync(req.LocationId, userId, ct);
        if (location == null)
        {
            await SendNotFoundAsync("Location not found", ct);
            return;
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = req.Name,
            Description = req.Description,
            Quantity = req.Quantity ?? 1,
            LocationId = req.LocationId
        };

        var created = await _itemRepository.CreateAsync(item, ct);

        var response = new ItemResponse
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            Quantity = created.Quantity,
            LocationId = created.LocationId,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };

        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        HttpContext.Response.Headers.Location = $"/api/items/{created.Id}";
        await HttpContext.Response.WriteAsJsonAsync(response, ct);
    }
}
