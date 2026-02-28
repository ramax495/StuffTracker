using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StuffTracker.Api.Features.Items.Shared;
using StuffTracker.Api.Features.Locations.GetLocation;
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;
using Testcontainers.PostgreSql;

namespace StuffTracker.Api.Integration.Locations;

/// <summary>
/// Integration tests for location move functionality with subtree path updates using PostgreSQL via Testcontainers.
/// These tests verify that moving a location correctly updates PathNames and Depth for the entire subtree.
/// </summary>
[Trait("Category", "Integration")]
public class MoveLocationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private IntegrationTestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private const long TestUserId = 999888777;
    private const long OtherUserId = 111222333;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgres.StartAsync();

        _factory = new IntegrationTestWebApplicationFactory(_postgres.GetConnectionString());

        var telegramUser = new TelegramUser
        {
            Id = TestUserId,
            FirstName = "Integration",
            LastName = "Test",
            Username = "integrationtest",
            LanguageCode = "en"
        };

        var mockValidationService = Substitute.For<ITelegramValidationService>();
        mockValidationService.Validate(Arg.Any<string>())
            .Returns(TelegramValidationResult.Success(telegramUser));

        _client = _factory.WithWebHostBuilder(builder =>
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

        _client.DefaultRequestHeaders.Add(TelegramAuthenticationHandler.HeaderName, "valid_init_data");

        // Run migrations
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private HttpClient CreateClientForUser(long userId)
    {
        var telegramUser = new TelegramUser
        {
            Id = userId,
            FirstName = "Other",
            LastName = "User",
            Username = "otheruser",
            LanguageCode = "en"
        };

        var mockValidationService = Substitute.For<ITelegramValidationService>();
        mockValidationService.Validate(Arg.Any<string>())
            .Returns(TelegramValidationResult.Success(telegramUser));

        var client = _factory.WithWebHostBuilder(builder =>
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
        return client;
    }

    private async Task<LocationResponse> CreateTestLocationAsync(HttpClient client, string name, Guid? parentId = null)
    {
        var request = parentId.HasValue
            ? new { Name = name, ParentId = parentId }
            : (object)new { Name = name };
        var response = await client.PostAsJsonAsync("/api/locations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<ItemResponse> CreateTestItemAsync(HttpClient client, string name, Guid locationId, string? description = null)
    {
        var request = new { Name = name, LocationId = locationId, Description = description };
        var response = await client.PostAsJsonAsync("/api/items", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    [Fact]
    public async Task MoveLocation_UpdatesPathNamesForLocation()
    {
        // Arrange - Create hierarchy
        var branchA = await CreateTestLocationAsync(_client, "Branch A");
        var branchAChild = await CreateTestLocationAsync(_client, "Branch A Child", branchA.Id);

        var branchB = await CreateTestLocationAsync(_client, "Branch B");

        // Verify initial path
        branchAChild.Breadcrumbs.Should().Equal("Branch A", "Branch A Child");

        // Act - Move child from Branch A to Branch B
        var moveRequest = new { ParentId = branchB.Id };
        var response = await _client.PostAsJsonAsync($"/api/locations/{branchAChild.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedLocation = await response.Content.ReadFromJsonAsync<LocationResponse>();
        movedLocation.Should().NotBeNull();
        movedLocation!.Breadcrumbs.Should().Equal("Branch B", "Branch A Child");

        // Verify persistence by fetching again
        var verifyResponse = await _client.GetAsync($"/api/locations/{branchAChild.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        verified!.Breadcrumbs.Should().Equal("Branch B", "Branch A Child");
    }

    [Fact]
    public async Task MoveLocation_UpdatesDepthForLocation()
    {
        // Arrange - Create deep hierarchy
        var level0 = await CreateTestLocationAsync(_client, "Level 0");
        var level1 = await CreateTestLocationAsync(_client, "Level 1", level0.Id);
        var level2 = await CreateTestLocationAsync(_client, "Level 2", level1.Id);
        var level3 = await CreateTestLocationAsync(_client, "Level 3", level2.Id);

        // Verify initial depths
        level3.Depth.Should().Be(3);

        // Act - Move level3 to root
        var moveRequest = new { ParentId = (Guid?)null };
        var response = await _client.PostAsJsonAsync($"/api/locations/{level3.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedLocation = await response.Content.ReadFromJsonAsync<LocationResponse>();
        movedLocation.Should().NotBeNull();
        movedLocation!.Depth.Should().Be(0);

        // Verify persistence
        var verifyResponse = await _client.GetAsync($"/api/locations/{level3.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        verified!.Depth.Should().Be(0);
    }

    [Fact]
    public async Task MoveLocation_UpdatesDescendantPaths()
    {
        // Arrange - Create hierarchy with multiple levels of descendants
        // Initial: Root -> Parent -> Child -> Grandchild
        var root = await CreateTestLocationAsync(_client, "Root");
        var parent = await CreateTestLocationAsync(_client, "Parent", root.Id);
        var child = await CreateTestLocationAsync(_client, "Child", parent.Id);
        var grandchild = await CreateTestLocationAsync(_client, "Grandchild", child.Id);

        // Create new target
        var newRoot = await CreateTestLocationAsync(_client, "New Root");

        // Verify initial paths
        grandchild.Breadcrumbs.Should().Equal("Root", "Parent", "Child", "Grandchild");

        // Act - Move "Parent" subtree to "New Root"
        var moveRequest = new { ParentId = newRoot.Id };
        var response = await _client.PostAsJsonAsync($"/api/locations/{parent.Id}/move", moveRequest);

        // Assert - Parent moved successfully
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedParent = await response.Content.ReadFromJsonAsync<LocationResponse>();
        movedParent!.Breadcrumbs.Should().Equal("New Root", "Parent");

        // Verify child path updated
        var childResponse = await _client.GetAsync($"/api/locations/{child.Id}");
        var childDetail = await childResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        childDetail!.Breadcrumbs.Should().Equal("New Root", "Parent", "Child");

        // Verify grandchild path updated
        var grandchildResponse = await _client.GetAsync($"/api/locations/{grandchild.Id}");
        var grandchildDetail = await grandchildResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        grandchildDetail!.Breadcrumbs.Should().Equal("New Root", "Parent", "Child", "Grandchild");
    }

    [Fact]
    public async Task MoveLocation_UpdatesDescendantDepths()
    {
        // Arrange - Create hierarchy: Root -> Parent -> Child -> Grandchild
        var root = await CreateTestLocationAsync(_client, "Root");
        var parent = await CreateTestLocationAsync(_client, "Parent", root.Id);
        var child = await CreateTestLocationAsync(_client, "Child", parent.Id);
        var grandchild = await CreateTestLocationAsync(_client, "Grandchild", child.Id);

        // Verify initial depths
        root.Depth.Should().Be(0);
        parent.Depth.Should().Be(1);
        child.Depth.Should().Be(2);
        grandchild.Depth.Should().Be(3);

        // Act - Move "Parent" subtree to root level
        var moveRequest = new { ParentId = (Guid?)null };
        var response = await _client.PostAsJsonAsync($"/api/locations/{parent.Id}/move", moveRequest);

        // Assert - Parent is now at root level
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedParent = await response.Content.ReadFromJsonAsync<LocationResponse>();
        movedParent!.Depth.Should().Be(0);

        // Verify child depth updated (was 2, now 1)
        var childResponse = await _client.GetAsync($"/api/locations/{child.Id}");
        var childDetail = await childResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        childDetail!.Depth.Should().Be(1);

        // Verify grandchild depth updated (was 3, now 2)
        var grandchildResponse = await _client.GetAsync($"/api/locations/{grandchild.Id}");
        var grandchildDetail = await grandchildResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        grandchildDetail!.Depth.Should().Be(2);
    }

    [Fact]
    public async Task MoveLocation_ToRoot_SetsCorrectPath()
    {
        // Arrange
        var parent = await CreateTestLocationAsync(_client, "Parent Level");
        var child = await CreateTestLocationAsync(_client, "Child Level", parent.Id);
        var grandchild = await CreateTestLocationAsync(_client, "Grandchild Level", child.Id);

        // Verify initial state
        grandchild.Breadcrumbs.Should().Equal("Parent Level", "Child Level", "Grandchild Level");
        grandchild.Depth.Should().Be(2);

        // Act - Move grandchild to root
        var moveRequest = new { ParentId = (Guid?)null };
        var response = await _client.PostAsJsonAsync($"/api/locations/{grandchild.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedLocation = await response.Content.ReadFromJsonAsync<LocationResponse>();
        movedLocation.Should().NotBeNull();

        // Root location should have single-element PathNames containing only its own name
        movedLocation!.Breadcrumbs.Should().HaveCount(1);
        movedLocation.Breadcrumbs.Should().Equal("Grandchild Level");
        movedLocation.ParentId.Should().BeNull();
        movedLocation.Depth.Should().Be(0);
    }

    [Fact]
    public async Task MoveLocation_PreservesItemsInSubtree()
    {
        // Arrange - Create hierarchy with items
        var root = await CreateTestLocationAsync(_client, "Root with Items");
        var parent = await CreateTestLocationAsync(_client, "Parent with Items", root.Id);
        var child = await CreateTestLocationAsync(_client, "Child with Items", parent.Id);

        // Create items in the subtree
        var parentItem = await CreateTestItemAsync(_client, "Parent Item", parent.Id, "Item in parent");
        var childItem = await CreateTestItemAsync(_client, "Child Item", child.Id, "Item in child");

        // Create new target
        var newTarget = await CreateTestLocationAsync(_client, "New Target");

        // Act - Move "Parent" subtree (with items) to "New Target"
        var moveRequest = new { ParentId = newTarget.Id };
        var response = await _client.PostAsJsonAsync($"/api/locations/{parent.Id}/move", moveRequest);

        // Assert - Move succeeded
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify items are still in their locations
        var parentDetailResponse = await _client.GetAsync($"/api/locations/{parent.Id}");
        var parentDetail = await parentDetailResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        parentDetail!.Items.Should().HaveCount(1);
        parentDetail.Items[0].Name.Should().Be("Parent Item");

        var childDetailResponse = await _client.GetAsync($"/api/locations/{child.Id}");
        var childDetail = await childDetailResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        childDetail!.Items.Should().HaveCount(1);
        childDetail.Items[0].Name.Should().Be("Child Item");

        // Verify item can still be fetched by ID
        var parentItemResponse = await _client.GetAsync($"/api/items/{parentItem.Id}");
        parentItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedParentItem = await parentItemResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        fetchedParentItem!.LocationId.Should().Be(parent.Id);
        // Verify the item shows the new location path
        fetchedParentItem.LocationPath.Should().Equal("New Target", "Parent with Items");

        var childItemResponse = await _client.GetAsync($"/api/items/{childItem.Id}");
        childItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedChildItem = await childItemResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        fetchedChildItem!.LocationId.Should().Be(child.Id);
        fetchedChildItem.LocationPath.Should().Equal("New Target", "Parent with Items", "Child with Items");
    }

    [Fact]
    public async Task MoveLocation_UserIsolation()
    {
        // Arrange - Create location hierarchy as primary user
        var userARoot = await CreateTestLocationAsync(_client, "User A Root");
        var userAChild = await CreateTestLocationAsync(_client, "User A Child", userARoot.Id);

        // Create client for other user
        using var otherClient = CreateClientForUser(OtherUserId);

        // Create location as other user
        var userBRoot = await CreateTestLocationAsync(otherClient, "User B Root");

        // Act - Try to move User A's location to User B's location
        var moveRequest = new { ParentId = userBRoot.Id };
        var response = await _client.PostAsJsonAsync($"/api/locations/{userAChild.Id}/move", moveRequest);

        // Assert - Should get 404 (target parent not found for this user)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify location is unchanged
        var verifyResponse = await _client.GetAsync($"/api/locations/{userAChild.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        verified!.ParentId.Should().Be(userARoot.Id);
        verified.Breadcrumbs.Should().Equal("User A Root", "User A Child");

        // Act 2 - Try to access User A's location as User B
        var response2 = await otherClient.PostAsJsonAsync($"/api/locations/{userAChild.Id}/move", new { ParentId = userBRoot.Id });

        // Assert - Should get 404 (location not found for other user)
        response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public IntegrationTestWebApplicationFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "test_bot_token_for_testing",
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                });
            });

            builder.ConfigureServices(services =>
            {
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
