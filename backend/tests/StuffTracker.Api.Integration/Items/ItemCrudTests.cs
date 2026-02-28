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

namespace StuffTracker.Api.Integration.Items;

/// <summary>
/// Integration tests for item CRUD operations using PostgreSQL via Testcontainers.
/// These tests require Docker to be running.
/// </summary>
[Trait("Category", "Integration")]
public class ItemCrudTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private IntegrationTestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private const long TestUserId = 999888777;

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

    [Fact]
    public async Task Create_Item_In_Location()
    {
        // Arrange - Create location
        var locationRequest = new { Name = "Storage Room" };
        var locationResponse = await _client.PostAsJsonAsync("/api/locations", locationRequest);
        locationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Act - Create item
        var itemRequest = new
        {
            Name = "Toolbox",
            Description = "Red metal toolbox",
            Quantity = 1,
            LocationId = location!.Id
        };
        var response = await _client.PostAsJsonAsync("/api/items", itemRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<ItemResponse>();
        item.Should().NotBeNull();
        item!.Name.Should().Be("Toolbox");
        item.Description.Should().Be("Red metal toolbox");
        item.Quantity.Should().Be(1);
        item.LocationId.Should().Be(location.Id);

        // Verify item appears in location details
        var locationDetailResponse = await _client.GetAsync($"/api/locations/{location.Id}");
        var locationDetail = await locationDetailResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        locationDetail!.Items.Should().HaveCount(1);
        locationDetail.Items[0].Name.Should().Be("Toolbox");
    }

    [Fact]
    public async Task Update_Item_Properties()
    {
        // Arrange - Create location and item
        var locationRequest = new { Name = "Bedroom" };
        var locationResponse = await _client.PostAsJsonAsync("/api/locations", locationRequest);
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>();

        var createItemRequest = new
        {
            Name = "Original Name",
            Description = "Original description",
            Quantity = 1,
            LocationId = location!.Id
        };
        var createResponse = await _client.PostAsJsonAsync("/api/items", createItemRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        // Act - Update all properties
        var updateRequest = new
        {
            Name = "Updated Name",
            Description = "Updated description",
            Quantity = 5
        };
        var updateResponse = await _client.PatchAsJsonAsync($"/api/items/{created!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ItemResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated description");
        updated.Quantity.Should().Be(5);
        updated.UpdatedAt.Should().BeAfter(created.CreatedAt);

        // Verify by fetching again
        var verifyResponse = await _client.GetAsync($"/api/items/{created.Id}");
        var verified = await verifyResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        verified!.Name.Should().Be("Updated Name");
        verified.Description.Should().Be("Updated description");
        verified.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task Delete_Item()
    {
        // Arrange - Create location and item
        var locationRequest = new { Name = "Garage" };
        var locationResponse = await _client.PostAsJsonAsync("/api/locations", locationRequest);
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>();

        var createItemRequest = new
        {
            Name = "Item to Delete",
            Quantity = 1,
            LocationId = location!.Id
        };
        var createResponse = await _client.PostAsJsonAsync("/api/items", createItemRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        // Act - Delete item
        var deleteResponse = await _client.DeleteAsync($"/api/items/{created!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify item is deleted
        var verifyResponse = await _client.GetAsync($"/api/items/{created.Id}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify item no longer appears in location
        var locationDetailResponse = await _client.GetAsync($"/api/locations/{location.Id}");
        var locationDetail = await locationDetailResponse.Content.ReadFromJsonAsync<LocationDetailResponse>();
        locationDetail!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Verify_Location_Path_Is_Correct()
    {
        // Arrange - Create nested hierarchy
        // Level 0: House
        var houseRequest = new { Name = "House" };
        var houseResponse = await _client.PostAsJsonAsync("/api/locations", houseRequest);
        var house = await houseResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Level 1: Kitchen
        var kitchenRequest = new { Name = "Kitchen", ParentId = house!.Id };
        var kitchenResponse = await _client.PostAsJsonAsync("/api/locations", kitchenRequest);
        var kitchen = await kitchenResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Level 2: Upper Cabinet
        var cabinetRequest = new { Name = "Upper Cabinet", ParentId = kitchen!.Id };
        var cabinetResponse = await _client.PostAsJsonAsync("/api/locations", cabinetRequest);
        var cabinet = await cabinetResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Create item in deepest location
        var itemRequest = new
        {
            Name = "Coffee Mugs",
            Description = "Set of ceramic mugs",
            Quantity = 6,
            LocationId = cabinet!.Id
        };
        var createResponse = await _client.PostAsJsonAsync("/api/items", itemRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        // Act - Get item with location path
        var response = await _client.GetAsync($"/api/items/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await response.Content.ReadFromJsonAsync<ItemDetailResponse>();

        item.Should().NotBeNull();
        item!.Name.Should().Be("Coffee Mugs");
        item.LocationPath.Should().Equal("House", "Kitchen", "Upper Cabinet");
        item.LocationName.Should().Be("Upper Cabinet");
        item.LocationId.Should().Be(cabinet.Id);
    }

    [Fact]
    public async Task Multiple_Items_In_Same_Location()
    {
        // Arrange - Create location
        var locationRequest = new { Name = "Office Desk" };
        var locationResponse = await _client.PostAsJsonAsync("/api/locations", locationRequest);
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Create multiple items
        var item1Request = new { Name = "Laptop", Quantity = 1, LocationId = location!.Id };
        var item2Request = new { Name = "Monitor", Quantity = 2, LocationId = location.Id };
        var item3Request = new { Name = "Keyboard", Quantity = 1, LocationId = location.Id };

        await _client.PostAsJsonAsync("/api/items", item1Request);
        await _client.PostAsJsonAsync("/api/items", item2Request);
        await _client.PostAsJsonAsync("/api/items", item3Request);

        // Act - Get location with items
        var response = await _client.GetAsync($"/api/locations/{location.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var locationDetail = await response.Content.ReadFromJsonAsync<LocationDetailResponse>();

        locationDetail.Should().NotBeNull();
        locationDetail!.Items.Should().HaveCount(3);
        locationDetail.Items.Should().Contain(i => i.Name == "Laptop");
        locationDetail.Items.Should().Contain(i => i.Name == "Monitor");
        locationDetail.Items.Should().Contain(i => i.Name == "Keyboard");
    }

    [Fact]
    public async Task Delete_Location_With_Items_Force()
    {
        // Arrange - Create location with items
        var locationRequest = new { Name = "Location with Items" };
        var locationResponse = await _client.PostAsJsonAsync("/api/locations", locationRequest);
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>();

        var itemRequest = new { Name = "Item in Location", Quantity = 1, LocationId = location!.Id };
        var createResponse = await _client.PostAsJsonAsync("/api/items", itemRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        // Try to delete without force - should fail
        var deleteNoForce = await _client.DeleteAsync($"/api/locations/{location.Id}");
        deleteNoForce.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Act - Delete with force
        var deleteWithForce = await _client.DeleteAsync($"/api/locations/{location.Id}?force=true");

        // Assert
        deleteWithForce.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify location is deleted
        var locationVerify = await _client.GetAsync($"/api/locations/{location.Id}");
        locationVerify.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify item is also deleted (cascade)
        var itemVerify = await _client.GetAsync($"/api/items/{created!.Id}");
        itemVerify.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
