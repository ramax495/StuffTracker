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
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Features.Search.SearchItems;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;
using Testcontainers.PostgreSql;

namespace StuffTracker.Api.Integration.Items;

/// <summary>
/// Integration tests for item move functionality using PostgreSQL via Testcontainers.
/// These tests verify item relocation across location hierarchies.
/// </summary>
[Trait("Category", "Integration")]
public class MoveItemTests : IAsyncLifetime
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
    public async Task Move_Item_To_Different_Location_Updates_LocationId()
    {
        // Arrange - Create hierarchy
        var sourceLocation = await CreateTestLocationAsync(_client, "Source Room");
        var targetLocation = await CreateTestLocationAsync(_client, "Target Room");

        var item = await CreateTestItemAsync(_client, "Portable Item", sourceLocation.Id, "Item to be moved");
        item.LocationId.Should().Be(sourceLocation.Id);

        var moveRequest = new { LocationId = targetLocation.Id };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedItem = await response.Content.ReadFromJsonAsync<ItemResponse>();
        movedItem.Should().NotBeNull();
        movedItem!.LocationId.Should().Be(targetLocation.Id);
        movedItem.Name.Should().Be("Portable Item");
        movedItem.Description.Should().Be("Item to be moved");
        movedItem.UpdatedAt.Should().BeOnOrAfter(item.UpdatedAt);

        // Verify persistence
        var verifyResponse = await _client.GetAsync($"/api/items/{item.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        verified!.LocationId.Should().Be(targetLocation.Id);
    }

    [Fact]
    public async Task Move_Item_Updates_Location_Path_In_Search_Results()
    {
        // Arrange - Create nested hierarchies
        var branchA = await CreateTestLocationAsync(_client, "Branch A");
        var branchAChild = await CreateTestLocationAsync(_client, "Branch A Child", branchA.Id);

        var branchB = await CreateTestLocationAsync(_client, "Branch B");
        var branchBChild = await CreateTestLocationAsync(_client, "Branch B Child", branchB.Id);

        var item = await CreateTestItemAsync(_client, "Moveable Widget", branchAChild.Id);

        // Verify initial location path in search
        var searchBefore = await _client.GetAsync($"/api/search/items?q=Moveable");
        var beforeResult = await searchBefore.Content.ReadFromJsonAsync<SearchResultsResponse>();
        beforeResult!.Items.Should().HaveCount(1);
        beforeResult.Items[0].LocationPath.Should().Equal("Branch A", "Branch A Child");

        // Act - Move to different branch
        var moveRequest = new { LocationId = branchBChild.Id };
        var moveResponse = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);
        moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Search should reflect new location path
        var searchAfter = await _client.GetAsync($"/api/search/items?q=Moveable");
        var afterResult = await searchAfter.Content.ReadFromJsonAsync<SearchResultsResponse>();
        afterResult!.Items.Should().HaveCount(1);
        afterResult.Items[0].LocationPath.Should().Equal("Branch B", "Branch B Child");
    }

    [Fact]
    public async Task Move_Item_Between_Different_Hierarchy_Branches()
    {
        // Arrange - Create two separate hierarchies
        var house = await CreateTestLocationAsync(_client, "House");
        var kitchen = await CreateTestLocationAsync(_client, "Kitchen", house.Id);
        var kitchenDrawer = await CreateTestLocationAsync(_client, "Kitchen Drawer", kitchen.Id);

        var office = await CreateTestLocationAsync(_client, "Office");
        var desk = await CreateTestLocationAsync(_client, "Desk", office.Id);
        var deskDrawer = await CreateTestLocationAsync(_client, "Desk Drawer", desk.Id);

        // Create item in kitchen hierarchy
        var item = await CreateTestItemAsync(_client, "USB Cable", kitchenDrawer.Id);

        // Verify item is in kitchen hierarchy
        var itemBefore = await _client.GetAsync($"/api/items/{item.Id}");
        var beforeDetail = await itemBefore.Content.ReadFromJsonAsync<ItemDetailResponse>();
        beforeDetail!.LocationPath.Should().Equal("House", "Kitchen", "Kitchen Drawer");

        // Act - Move to office hierarchy
        var moveRequest = new { LocationId = deskDrawer.Id };
        var moveResponse = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var itemAfter = await _client.GetAsync($"/api/items/{item.Id}");
        var afterDetail = await itemAfter.Content.ReadFromJsonAsync<ItemDetailResponse>();
        afterDetail!.LocationPath.Should().Equal("Office", "Desk", "Desk Drawer");
        afterDetail.LocationName.Should().Be("Desk Drawer");
    }

    [Fact]
    public async Task Cannot_Move_Item_To_Non_Existent_Location()
    {
        // Arrange
        var location = await CreateTestLocationAsync(_client, "Valid Location");
        var item = await CreateTestItemAsync(_client, "Item with Valid Location", location.Id);
        var nonExistentLocationId = Guid.NewGuid();

        var moveRequest = new { LocationId = nonExistentLocationId };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify item location unchanged
        var verifyResponse = await _client.GetAsync($"/api/items/{item.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        verified!.LocationId.Should().Be(location.Id);
    }

    [Fact]
    public async Task Cannot_Move_Another_Users_Item()
    {
        // Arrange - Create location and item as primary user
        var location = await CreateTestLocationAsync(_client, "User A Location");
        var targetLocation = await CreateTestLocationAsync(_client, "User A Target");
        var item = await CreateTestItemAsync(_client, "User A Item", location.Id);

        // Create client for other user
        using var otherClient = CreateClientForUser(OtherUserId);

        // Create a location as the other user (to provide valid target)
        var otherUserLocation = await CreateTestLocationAsync(otherClient, "User B Location");

        // Act - Try to move User A's item as User B
        var moveRequest = new { LocationId = otherUserLocation.Id };
        var response = await otherClient.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert - Should get 404 (item not found for this user)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify item is still in original location
        var verifyResponse = await _client.GetAsync($"/api/items/{item.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        verified!.LocationId.Should().Be(location.Id);
    }

    [Fact]
    public async Task Cannot_Move_Item_To_Another_Users_Location()
    {
        // Arrange - Create location and item as primary user
        var userALocation = await CreateTestLocationAsync(_client, "User A Location");
        var item = await CreateTestItemAsync(_client, "User A Item", userALocation.Id);

        // Create client for other user and create a location
        using var otherClient = CreateClientForUser(OtherUserId);
        var userBLocation = await CreateTestLocationAsync(otherClient, "User B Location");

        // Act - Try to move User A's item to User B's location
        var moveRequest = new { LocationId = userBLocation.Id };
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert - Should get 404 (target location not found for this user)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify item is still in original location
        var verifyResponse = await _client.GetAsync($"/api/items/{item.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        verified!.LocationId.Should().Be(userALocation.Id);
    }

    [Fact]
    public async Task Move_Item_To_Same_Location_Is_Idempotent()
    {
        // Arrange
        var location = await CreateTestLocationAsync(_client, "Same Location");
        var item = await CreateTestItemAsync(_client, "Stationary Item", location.Id);

        var moveRequest = new { LocationId = location.Id };

        // Act - Move to same location
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert - Should succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedItem = await response.Content.ReadFromJsonAsync<ItemResponse>();
        movedItem!.LocationId.Should().Be(location.Id);
    }

    [Fact]
    public async Task Move_Item_From_Child_To_Parent_Location()
    {
        // Arrange
        var parent = await CreateTestLocationAsync(_client, "Parent Location");
        var child = await CreateTestLocationAsync(_client, "Child Location", parent.Id);

        var item = await CreateTestItemAsync(_client, "Item in Child", child.Id);

        // Act - Move from child to parent
        var moveRequest = new { LocationId = parent.Id };
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedItem = await response.Content.ReadFromJsonAsync<ItemResponse>();
        movedItem!.LocationId.Should().Be(parent.Id);

        // Verify location path updated
        var itemDetail = await _client.GetAsync($"/api/items/{item.Id}");
        var detail = await itemDetail.Content.ReadFromJsonAsync<ItemDetailResponse>();
        detail!.LocationPath.Should().Equal("Parent Location");
    }

    [Fact]
    public async Task Move_Item_From_Parent_To_Child_Location()
    {
        // Arrange
        var parent = await CreateTestLocationAsync(_client, "Parent Location");
        var child = await CreateTestLocationAsync(_client, "Child Location", parent.Id);

        var item = await CreateTestItemAsync(_client, "Item in Parent", parent.Id);

        // Act - Move from parent to child
        var moveRequest = new { LocationId = child.Id };
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movedItem = await response.Content.ReadFromJsonAsync<ItemResponse>();
        movedItem!.LocationId.Should().Be(child.Id);

        // Verify location path updated
        var itemDetail = await _client.GetAsync($"/api/items/{item.Id}");
        var detail = await itemDetail.Content.ReadFromJsonAsync<ItemDetailResponse>();
        detail!.LocationPath.Should().Equal("Parent Location", "Child Location");
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
