using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StuffTracker.Api.Common;
using StuffTracker.Api.Features.Locations.GetLocation;
using StuffTracker.Api.Features.Locations.GetLocationTree;
using StuffTracker.Api.Features.Locations.GetTopLevelLocations;
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Contract.Locations;

public class LocationEndpointTests
{
    private const long TestUserId = 123456789;

    private (HttpClient Client, TestWebApplicationFactory Factory) CreateAuthenticatedClientAndFactory()
    {
        var factory = new TestWebApplicationFactory();
        var telegramUser = new TelegramUser
        {
            Id = TestUserId,
            FirstName = "Test",
            LastName = "User",
            Username = "testuser",
            LanguageCode = "en"
        };

        var mockValidationService = Substitute.For<ITelegramValidationService>();
        mockValidationService.Validate(Arg.Any<string>())
            .Returns(TelegramValidationResult.Success(telegramUser));

        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ITelegramValidationService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockValidationService);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add(TelegramAuthenticationHandler.HeaderName, "valid_init_data");
        return (client, factory);
    }

    [Fact]
    public async Task GET_Locations_Returns_Empty_Array_When_No_Locations()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Act
        var response = await client.GetAsync("/api/locations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var locations = await response.Content.ReadFromJsonAsync<List<LocationListItemResponse>>();
        locations.Should().NotBeNull();
        locations.Should().BeEmpty();
    }

    [Fact]
    public async Task POST_Locations_Creates_TopLevel_Location()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;
        var request = new { Name = "Living Room" };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var location = await response.Content.ReadFromJsonAsync<LocationResponse>();
        location.Should().NotBeNull();
        location!.Name.Should().Be("Living Room");
        location.ParentId.Should().BeNull();
        location.Depth.Should().Be(0);
        location.Breadcrumbs.Should().Contain("Living Room");
    }

    [Fact]
    public async Task POST_Locations_Creates_Child_Location()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create parent first
        var parentRequest = new { Name = "House" };
        var parentResponse = await client.PostAsJsonAsync("/api/locations", parentRequest);
        var parent = await parentResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Create child
        var childRequest = new { Name = "Bedroom", ParentId = parent!.Id };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", childRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var child = await response.Content.ReadFromJsonAsync<LocationResponse>();
        child.Should().NotBeNull();
        child!.Name.Should().Be("Bedroom");
        child.ParentId.Should().Be(parent.Id);
        child.Depth.Should().Be(1);
        child.Breadcrumbs.Should().ContainInOrder("House", "Bedroom");
    }

    [Fact]
    public async Task POST_Locations_Returns_400_When_Name_Empty()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;
        var request = new { Name = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Locations_Returns_404_When_Parent_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;
        var request = new { Name = "Test", ParentId = Guid.NewGuid() };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Locations_Id_Returns_Location_With_Children_And_Items()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create parent
        var parentRequest = new { Name = "Garage" };
        var parentResponse = await client.PostAsJsonAsync("/api/locations", parentRequest);
        var parent = await parentResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Create child
        var childRequest = new { Name = "Shelf 1", ParentId = parent!.Id };
        await client.PostAsJsonAsync("/api/locations", childRequest);

        // Act
        var response = await client.GetAsync($"/api/locations/{parent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var location = await response.Content.ReadFromJsonAsync<LocationDetailResponse>();
        location.Should().NotBeNull();
        location!.Id.Should().Be(parent.Id);
        location.Name.Should().Be("Garage");
        location.Children.Should().HaveCount(1);
        location.Children[0].Name.Should().Be("Shelf 1");
        location.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GET_Locations_Id_Returns_404_When_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Act
        var response = await client.GetAsync($"/api/locations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_Locations_Id_Updates_Name()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create location
        var createRequest = new { Name = "Old Name" };
        var createResponse = await client.PostAsJsonAsync("/api/locations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Update
        var updateRequest = new { Name = "New Name" };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/locations/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<LocationResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
        updated.Breadcrumbs.Should().Contain("New Name");
        updated.UpdatedAt.Should().BeOnOrAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task PATCH_Locations_Id_Returns_404_When_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;
        var updateRequest = new { Name = "New Name" };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/locations/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Locations_Id_Deletes_Empty_Location()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create location
        var createRequest = new { Name = "To Delete" };
        var createResponse = await client.PostAsJsonAsync("/api/locations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Act
        var response = await client.DeleteAsync($"/api/locations/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/locations/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Locations_Id_Returns_409_When_Has_Children()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create parent with child
        var parentRequest = new { Name = "Parent" };
        var parentResponse = await client.PostAsJsonAsync("/api/locations", parentRequest);
        var parent = await parentResponse.Content.ReadFromJsonAsync<LocationResponse>();

        var childRequest = new { Name = "Child", ParentId = parent!.Id };
        await client.PostAsJsonAsync("/api/locations", childRequest);

        // Act
        var response = await client.DeleteAsync($"/api/locations/{parent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var conflict = await response.Content.ReadFromJsonAsync<LocationDeleteConflictResponse>();
        conflict.Should().NotBeNull();
        conflict!.ChildCount.Should().Be(1);
    }

    [Fact]
    public async Task DELETE_Locations_Id_Force_Deletes_With_Children()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create parent with child
        var parentRequest = new { Name = "Parent Force" };
        var parentResponse = await client.PostAsJsonAsync("/api/locations", parentRequest);
        var parent = await parentResponse.Content.ReadFromJsonAsync<LocationResponse>();

        var childRequest = new { Name = "Child Force", ParentId = parent!.Id };
        await client.PostAsJsonAsync("/api/locations", childRequest);

        // Act
        var response = await client.DeleteAsync($"/api/locations/{parent.Id}?force=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/locations/{parent.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Locations_Tree_Returns_Hierarchical_Structure()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create hierarchy
        var rootRequest = new { Name = "Root Tree" };
        var rootResponse = await client.PostAsJsonAsync("/api/locations", rootRequest);
        var root = await rootResponse.Content.ReadFromJsonAsync<LocationResponse>();

        var child1Request = new { Name = "Child 1", ParentId = root!.Id };
        var child1Response = await client.PostAsJsonAsync("/api/locations", child1Request);
        var child1 = await child1Response.Content.ReadFromJsonAsync<LocationResponse>();

        var child2Request = new { Name = "Child 2", ParentId = root.Id };
        await client.PostAsJsonAsync("/api/locations", child2Request);

        var grandchildRequest = new { Name = "Grandchild", ParentId = child1!.Id };
        await client.PostAsJsonAsync("/api/locations", grandchildRequest);

        // Act
        var response = await client.GetAsync("/api/locations/tree");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tree = await response.Content.ReadFromJsonAsync<List<LocationTreeNodeResponse>>();
        tree.Should().NotBeNull();

        // Find our root in the tree
        var rootNode = tree!.FirstOrDefault(n => n.Name == "Root Tree");
        rootNode.Should().NotBeNull();
        rootNode!.Children.Should().HaveCount(2);

        var child1Node = rootNode.Children.FirstOrDefault(n => n.Name == "Child 1");
        child1Node.Should().NotBeNull();
        child1Node!.Children.Should().HaveCount(1);
        child1Node.Children[0].Name.Should().Be("Grandchild");
    }

    [Fact]
    public async Task GET_Locations_Returns_401_When_Unauthorized()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        // Note: Not adding auth header

        // Act
        var response = await client.GetAsync("/api/locations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "test_bot_token_for_testing",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove all AppDbContext related registrations
                var dbContextDescriptors = services.Where(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                         d.ServiceType == typeof(AppDbContext) ||
                         d.ServiceType == typeof(DbContextOptions)).ToList();
                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });

                // Remove the real TelegramValidationService and add a default mock
                var validationDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ITelegramValidationService));
                if (validationDescriptor != null)
                {
                    services.Remove(validationDescriptor);
                }

                var defaultMock = Substitute.For<ITelegramValidationService>();
                defaultMock.Validate(Arg.Any<string>())
                    .Returns(TelegramValidationResult.Failed("Test default - no auth configured"));
                services.AddSingleton(defaultMock);
            });

            builder.UseEnvironment("Development");
        }
    }
}
