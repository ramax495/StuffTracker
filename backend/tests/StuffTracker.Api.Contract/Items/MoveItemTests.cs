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
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Contract.Items;

public class MoveItemTests
{
    private const long TestUserId = 123456789;
    private const long OtherUserId = 987654321;

    private (HttpClient Client, TestWebApplicationFactory Factory) CreateAuthenticatedClientAndFactory(long userId = TestUserId)
    {
        var factory = new TestWebApplicationFactory();
        var telegramUser = new TelegramUser
        {
            Id = userId,
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

    private async Task<LocationResponse> CreateTestLocationAsync(HttpClient client, string name = "Test Location", Guid? parentId = null)
    {
        var request = parentId.HasValue
            ? new { Name = name, ParentId = parentId.Value }
            : (object)new { Name = name };
        var response = await client.PostAsJsonAsync("/api/locations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<ItemResponse> CreateTestItemAsync(HttpClient client, Guid locationId, string name = "Test Item")
    {
        var request = new { Name = name, LocationId = locationId };
        var response = await client.PostAsJsonAsync("/api/items", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    [Fact]
    public async Task POST_Items_Id_Move_Moves_Item_To_New_Location()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var sourceLocation = await CreateTestLocationAsync(client, "Source Location");
        var targetLocation = await CreateTestLocationAsync(client, "Target Location");
        var item = await CreateTestItemAsync(client, sourceLocation.Id, "Item to Move");

        var moveRequest = new { LocationId = targetLocation.Id };

        // Act
        var response = await client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var movedItem = await response.Content.ReadFromJsonAsync<ItemResponse>();
        movedItem.Should().NotBeNull();
        movedItem!.Id.Should().Be(item.Id);
        movedItem.Name.Should().Be("Item to Move");
        movedItem.LocationId.Should().Be(targetLocation.Id);
        movedItem.UpdatedAt.Should().BeOnOrAfter(item.UpdatedAt);
    }

    [Fact]
    public async Task POST_Items_Id_Move_Returns_Updated_Item_With_New_LocationId()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var originalLocation = await CreateTestLocationAsync(client, "Original");
        var newLocation = await CreateTestLocationAsync(client, "New");
        var item = await CreateTestItemAsync(client, originalLocation.Id, "My Item");

        // Verify original locationId
        item.LocationId.Should().Be(originalLocation.Id);

        var moveRequest = new { LocationId = newLocation.Id };

        // Act
        var response = await client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedItem = await response.Content.ReadFromJsonAsync<ItemResponse>();
        updatedItem.Should().NotBeNull();
        updatedItem!.LocationId.Should().Be(newLocation.Id);
        updatedItem.LocationId.Should().NotBe(originalLocation.Id);

        // Verify persistence by fetching again
        var getResponse = await client.GetAsync($"/api/items/{item.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedItem = await getResponse.Content.ReadFromJsonAsync<ItemDetailResponse>();
        fetchedItem!.LocationId.Should().Be(newLocation.Id);
    }

    [Fact]
    public async Task POST_Items_Id_Move_Returns_404_When_Item_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var nonExistentItemId = Guid.NewGuid();
        var moveRequest = new { LocationId = location.Id };

        // Act
        var response = await client.PostAsJsonAsync($"/api/items/{nonExistentItemId}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Items_Id_Move_Returns_404_When_Target_Location_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var item = await CreateTestItemAsync(client, location.Id);
        var nonExistentLocationId = Guid.NewGuid();

        var moveRequest = new { LocationId = nonExistentLocationId };

        // Act
        var response = await client.PostAsJsonAsync($"/api/items/{item.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Items_Id_Move_Returns_400_When_LocationId_Missing()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var item = await CreateTestItemAsync(client, location.Id);

        var emptyRequest = new { };

        // Act
        var response = await client.PostAsJsonAsync($"/api/items/{item.Id}/move", emptyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Items_Id_Move_Returns_400_When_LocationId_Empty_Guid()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var item = await CreateTestItemAsync(client, location.Id);

        var request = new { LocationId = Guid.Empty };

        // Act
        var response = await client.PostAsJsonAsync($"/api/items/{item.Id}/move", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Items_Id_Move_Returns_401_When_Unauthorized()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        // Note: Not adding auth header

        var moveRequest = new { LocationId = Guid.NewGuid() };

        // Act
        var response = await client.PostAsJsonAsync($"/api/items/{Guid.NewGuid()}/move", moveRequest);

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
