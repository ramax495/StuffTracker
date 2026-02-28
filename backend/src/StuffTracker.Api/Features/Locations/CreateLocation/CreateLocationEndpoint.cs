using Microsoft.AspNetCore.Http;
using StuffTracker.Api.Common;
using StuffTracker.Api.Domain;
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Locations.CreateLocation;

/// <summary>
/// Endpoint for creating a new location.
/// POST /api/locations
/// </summary>
public class CreateLocationEndpoint : BaseEndpoint<CreateLocationRequest, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;

    public CreateLocationEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Post("/locations");
        Summary(s =>
        {
            s.Summary = "Create a new location";
            s.Description = "Creates a new storage location for the authenticated user. Can be a top-level location or a child of an existing location.";
            s.Responses[201] = "Location created successfully";
            s.Responses[400] = "Validation error";
            s.Responses[401] = "Invalid or missing Telegram authentication";
            s.Responses[404] = "Parent location not found";
        });
    }

    public override async Task HandleAsync(CreateLocationRequest req, CancellationToken ct)
    {
        var userId = GetUserId();

        // Validate parent location exists and belongs to user
        if (req.ParentId.HasValue)
        {
            var parent = await _locationRepository.GetByIdAsync(req.ParentId.Value, userId, ct);
            if (parent == null)
            {
                await SendNotFoundAsync("Parent location not found", ct);
                return;
            }
        }

        var location = new StorageLocation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = req.Name,
            ParentId = req.ParentId
        };

        var created = await _locationRepository.CreateAsync(location, ct);

        // Ensure BreadcrumbIds is populated (fallback for edge cases)
        var breadcrumbIds = created.PathIds ?? Array.Empty<Guid>();
        if (breadcrumbIds.Length == 0)
        {
            // For newly created locations, always ensure at least the location's own ID is included
            breadcrumbIds = new[] { created.Id };
        }

        var response = new LocationResponse
        {
            Id = created.Id,
            Name = created.Name,
            ParentId = created.ParentId,
            Breadcrumbs = created.PathNames,
            BreadcrumbIds = breadcrumbIds,
            Depth = created.Depth,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };

        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        HttpContext.Response.Headers.Location = $"/api/locations/{created.Id}";
        await HttpContext.Response.WriteAsJsonAsync(response, ct);
    }
}
