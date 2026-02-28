using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StuffTracker.Api.Features.Locations.Shared;
using StuffTracker.Api.Features.Search.SearchItems;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Contract.Search;

public class SearchItemsTests
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

    private async Task<LocationResponse> CreateTestLocationAsync(HttpClient client, string name, Guid? parentId = null)
    {
        var request = parentId.HasValue
            ? new { Name = name, ParentId = parentId }
            : (object)new { Name = name };
        var response = await client.PostAsJsonAsync("/api/locations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<Guid> CreateTestItemAsync(HttpClient client, string name, Guid locationId, string? description = null)
    {
        var request = new { Name = name, LocationId = locationId, Description = description };
        var response = await client.PostAsJsonAsync("/api/items", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ItemCreatedResponse>();
        return result!.Id;
    }

    [Fact]
    public async Task GET_Search_Items_Returns_Results()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client, "Storage");
        await CreateTestItemAsync(client, "Hammer", location.Id);
        await CreateTestItemAsync(client, "Screwdriver", location.Id);

        // Act
        var response = await client.GetAsync("/api/search/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GET_Search_Items_With_Query_Filters_By_Name()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client, "Toolbox");
        await CreateTestItemAsync(client, "Hammer", location.Id);
        await CreateTestItemAsync(client, "Screwdriver", location.Id);
        await CreateTestItemAsync(client, "Nails", location.Id);

        // Act
        var response = await client.GetAsync("/api/search/items?q=hammer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Hammer");
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task GET_Search_Items_Query_Is_Case_Insensitive()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client, "Tools");
        await CreateTestItemAsync(client, "HAMMER", location.Id);

        // Act
        var response = await client.GetAsync("/api/search/items?q=hammer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("HAMMER");
    }

    [Fact]
    public async Task GET_Search_Items_With_LocationId_Filters_By_Location()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location1 = await CreateTestLocationAsync(client, "Garage");
        var location2 = await CreateTestLocationAsync(client, "Kitchen");
        await CreateTestItemAsync(client, "Wrench", location1.Id);
        await CreateTestItemAsync(client, "Knife", location2.Id);

        // Act
        var response = await client.GetAsync($"/api/search/items?locationId={location1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Wrench");
    }

    [Fact]
    public async Task GET_Search_Items_With_Pagination()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client, "Storage");
        for (int i = 1; i <= 10; i++)
        {
            await CreateTestItemAsync(client, $"Item{i:D2}", location.Id);
        }

        // Act - Get first page
        var response1 = await client.GetAsync("/api/search/items?limit=5&offset=0");
        var result1 = await response1.Content.ReadFromJsonAsync<SearchResultsResponse>();

        // Act - Get second page
        var response2 = await client.GetAsync("/api/search/items?limit=5&offset=5");
        var result2 = await response2.Content.ReadFromJsonAsync<SearchResultsResponse>();

        // Assert
        result1.Should().NotBeNull();
        result1!.Items.Should().HaveCount(5);
        result1.Total.Should().Be(10);
        result1.HasMore.Should().BeTrue();

        result2.Should().NotBeNull();
        result2!.Items.Should().HaveCount(5);
        result2.Total.Should().Be(10);
        result2.HasMore.Should().BeFalse();

        // Ensure no overlap between pages
        var page1Names = result1.Items.Select(i => i.Name).ToList();
        var page2Names = result2.Items.Select(i => i.Name).ToList();
        page1Names.Should().NotIntersectWith(page2Names);
    }

    [Fact]
    public async Task GET_Search_Items_Returns_LocationPath_In_Results()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var parent = await CreateTestLocationAsync(client, "House");
        var child = await CreateTestLocationAsync(client, "Kitchen", parent.Id);
        await CreateTestItemAsync(client, "Spatula", child.Id);

        // Act
        var response = await client.GetAsync("/api/search/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].LocationPath.Should().ContainInOrder("House", "Kitchen");
    }

    [Fact]
    public async Task GET_Search_Items_Returns_Empty_For_No_Matches()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client, "Storage");
        await CreateTestItemAsync(client, "Hammer", location.Id);

        // Act
        var response = await client.GetAsync("/api/search/items?q=nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GET_Search_Items_Returns_401_When_Unauthorized()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        // Note: Not adding auth header

        // Act
        var response = await client.GetAsync("/api/search/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Search_Items_Returns_404_For_Invalid_LocationId()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Act
        var response = await client.GetAsync($"/api/search/items?locationId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Search_Items_Returns_400_For_Invalid_Limit()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Act
        var response = await client.GetAsync("/api/search/items?limit=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Search_Items_Returns_400_For_Negative_Offset()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Act
        var response = await client.GetAsync("/api/search/items?offset=-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Search_Items_Combined_Query_And_Location_Filter()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var garage = await CreateTestLocationAsync(client, "Garage");
        var kitchen = await CreateTestLocationAsync(client, "Kitchen");
        await CreateTestItemAsync(client, "Red Hammer", garage.Id);
        await CreateTestItemAsync(client, "Blue Hammer", kitchen.Id);
        await CreateTestItemAsync(client, "Wrench", garage.Id);

        // Act
        var response = await client.GetAsync($"/api/search/items?q=hammer&locationId={garage.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Red Hammer");
    }

    private class ItemCreatedResponse
    {
        public Guid Id { get; set; }
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
