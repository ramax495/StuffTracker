using StuffTracker.Api.Common;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Features.Locations.GetLocationTree;

/// <summary>
/// Endpoint for getting the full location tree.
/// GET /api/locations/tree
/// </summary>
public class GetLocationTreeEndpoint : BaseEndpointWithoutRequest<List<LocationTreeNodeResponse>>
{
    private readonly ILocationRepository _locationRepository;

    public GetLocationTreeEndpoint(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public override void Configure()
    {
        Get("/locations/tree");
        Summary(s =>
        {
            s.Summary = "Get location tree";
            s.Description = "Returns the full hierarchical tree of all locations for the authenticated user.";
            s.Responses[200] = "Returns array of root tree nodes with nested children";
            s.Responses[401] = "Invalid or missing Telegram authentication";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = GetUserId();

        var flatList = await _locationRepository.GetTreeAsync(userId, ct);

        // Build tree from flat list
        Response = BuildTree(flatList);
    }

    /// <summary>
    /// Builds a hierarchical tree from flat list of location results.
    /// </summary>
    private static List<LocationTreeNodeResponse> BuildTree(List<LocationTreeResult> flatList)
    {
        var nodeMap = flatList.ToDictionary(
            l => l.Id,
            l => new LocationTreeNodeResponse
            {
                Id = l.Id,
                Name = l.Name,
                Depth = l.Depth,
                Children = new List<LocationTreeNodeResponse>()
            });

        var roots = new List<LocationTreeNodeResponse>();

        foreach (var location in flatList)
        {
            var node = nodeMap[location.Id];

            if (location.ParentId.HasValue && nodeMap.TryGetValue(location.ParentId.Value, out var parentNode))
            {
                parentNode.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        // Sort children by name at each level
        SortChildrenRecursively(roots);

        return roots;
    }

    private static void SortChildrenRecursively(List<LocationTreeNodeResponse> nodes)
    {
        nodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                SortChildrenRecursively(node.Children);
            }
        }
    }
}
