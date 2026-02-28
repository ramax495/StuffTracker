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
using StuffTracker.Api.Features.Items.Shared;
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Contract.Items;

public class ItemEndpointTests
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

    private async Task<LocationResponse> CreateTestLocationAsync(HttpClient client, string name = "Test Location")
    {
        var request = new { Name = name };
        var response = await client.PostAsJsonAsync("/api/locations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    [Fact]
    public async Task POST_Items_Creates_Item()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var request = new { Name = "Test Item", Description = "A test item", Quantity = 3, LocationId = location.Id };

        // Act
        var response = await client.PostAsJsonAsync("/api/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var item = await response.Content.ReadFromJsonAsync<ItemResponse>();
        item.Should().NotBeNull();
        item!.Name.Should().Be("Test Item");
        item.Description.Should().Be("A test item");
        item.Quantity.Should().Be(3);
        item.LocationId.Should().Be(location.Id);
    }

    [Fact]
    public async Task POST_Items_Creates_Item_With_Default_Quantity()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var request = new { Name = "Single Item", LocationId = location.Id };

        // Act
        var response = await client.PostAsJsonAsync("/api/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var item = await response.Content.ReadFromJsonAsync<ItemResponse>();
        item.Should().NotBeNull();
        item!.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task POST_Items_Returns_400_When_Name_Empty()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var request = new { Name = "", LocationId = location.Id };

        // Act
        var response = await client.PostAsJsonAsync("/api/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Items_Returns_400_When_Quantity_Zero()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var request = new { Name = "Invalid Item", Quantity = 0, LocationId = location.Id };

        // Act
        var response = await client.PostAsJsonAsync("/api/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Items_Returns_404_When_Location_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var request = new { Name = "Orphan Item", LocationId = Guid.NewGuid() };

        // Act
        var response = await client.PostAsJsonAsync("/api/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Items_Id_Returns_Item_With_Location_Path()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create nested locations
        var parentLocation = await CreateTestLocationAsync(client, "House");
        var childLocationRequest = new { Name = "Kitchen", ParentId = parentLocation.Id };
        var childResponse = await client.PostAsJsonAsync("/api/locations", childLocationRequest);
        var childLocation = await childResponse.Content.ReadFromJsonAsync<LocationResponse>();

        // Create item in child location
        var itemRequest = new { Name = "Coffee Mug", Quantity = 2, LocationId = childLocation!.Id };
        var createResponse = await client.PostAsJsonAsync("/api/items", itemRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        // Act
        var response = await client.GetAsync($"/api/items/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var item = await response.Content.ReadFromJsonAsync<ItemDetailResponse>();
        item.Should().NotBeNull();
        item!.Id.Should().Be(created.Id);
        item.Name.Should().Be("Coffee Mug");
        item.Quantity.Should().Be(2);
        item.LocationPath.Should().ContainInOrder("House", "Kitchen");
        item.LocationName.Should().Be("Kitchen");
    }

    [Fact]
    public async Task GET_Items_Id_Returns_404_When_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Act
        var response = await client.GetAsync($"/api/items/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_Items_Id_Updates_Name()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var createRequest = new { Name = "Old Name", LocationId = location.Id };
        var createResponse = await client.PostAsJsonAsync("/api/items", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        var updateRequest = new { Name = "New Name" };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/items/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ItemResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
        updated.UpdatedAt.Should().BeOnOrAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task PATCH_Items_Id_Updates_Quantity()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var createRequest = new { Name = "Item", Quantity = 1, LocationId = location.Id };
        var createResponse = await client.PostAsJsonAsync("/api/items", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        var updateRequest = new { Quantity = 5 };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/items/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ItemResponse>();
        updated.Should().NotBeNull();
        updated!.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task PATCH_Items_Id_Updates_Description()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var createRequest = new { Name = "Item", LocationId = location.Id };
        var createResponse = await client.PostAsJsonAsync("/api/items", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        var updateRequest = new { Description = "New description" };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/items/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ItemResponse>();
        updated.Should().NotBeNull();
        updated!.Description.Should().Be("New description");
    }

    [Fact]
    public async Task PATCH_Items_Id_Returns_404_When_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var updateRequest = new { Name = "New Name" };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/items/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_Items_Id_Returns_400_When_Quantity_Zero()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var createRequest = new { Name = "Item", LocationId = location.Id };
        var createResponse = await client.PostAsJsonAsync("/api/items", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        var updateRequest = new { Quantity = 0 };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/items/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DELETE_Items_Id_Deletes_Item()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client);
        var createRequest = new { Name = "To Delete", LocationId = location.Id };
        var createResponse = await client.PostAsJsonAsync("/api/items", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        // Act
        var response = await client.DeleteAsync($"/api/items/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/items/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Items_Id_Returns_404_When_Not_Found()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Act
        var response = await client.DeleteAsync($"/api/items/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Items_Returns_401_When_Unauthorized()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        // Note: Not adding auth header

        // Act
        var response = await client.GetAsync($"/api/items/{Guid.NewGuid()}");

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
