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
using Testcontainers.PostgreSql;

namespace StuffTracker.Api.Integration.Search;

/// <summary>
/// Integration tests for search functionality using PostgreSQL via Testcontainers.
/// These tests verify pg_trgm extension and ILIKE search behavior.
/// </summary>
[Trait("Category", "Integration")]
public class SearchIntegrationTests : IAsyncLifetime
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

    private async Task<LocationResponse> CreateTestLocationAsync(string name, Guid? parentId = null)
    {
        var request = parentId.HasValue
            ? new { Name = name, ParentId = parentId }
            : (object)new { Name = name };
        var response = await _client.PostAsJsonAsync("/api/locations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<Guid> CreateTestItemAsync(string name, Guid locationId, string? description = null)
    {
        var request = new { Name = name, LocationId = locationId, Description = description };
        var response = await _client.PostAsJsonAsync("/api/items", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ItemCreatedResponse>();
        return result!.Id;
    }

    [Fact]
    public async Task Search_Finds_Items_By_Partial_Name_Match()
    {
        // Arrange
        var location = await CreateTestLocationAsync("Workshop");
        await CreateTestItemAsync("Phillips Screwdriver", location.Id);
        await CreateTestItemAsync("Flathead Screwdriver", location.Id);
        await CreateTestItemAsync("Hammer", location.Id);

        // Act - Search for "screw"
        var response = await _client.GetAsync("/api/search/items?q=screw");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "Phillips Screwdriver");
        result.Items.Should().Contain(i => i.Name == "Flathead Screwdriver");
        result.Items.Should().NotContain(i => i.Name == "Hammer");
    }

    [Fact]
    public async Task Search_Is_Case_Insensitive()
    {
        // Arrange
        var location = await CreateTestLocationAsync("Garage");
        await CreateTestItemAsync("UPPER CASE ITEM", location.Id);
        await CreateTestItemAsync("lower case item", location.Id);
        await CreateTestItemAsync("Mixed Case Item", location.Id);

        // Act - Search with lowercase
        var response = await _client.GetAsync("/api/search/items?q=case");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items.Should().Contain(i => i.Name == "UPPER CASE ITEM");
        result.Items.Should().Contain(i => i.Name == "lower case item");
        result.Items.Should().Contain(i => i.Name == "Mixed Case Item");
    }

    [Fact]
    public async Task Search_With_Mixed_Case_Query()
    {
        // Arrange
        var location = await CreateTestLocationAsync("Office");
        await CreateTestItemAsync("Computer Monitor", location.Id);
        await CreateTestItemAsync("Computer Mouse", location.Id);
        await CreateTestItemAsync("Keyboard", location.Id);

        // Act - Search with mixed case query
        var response = await _client.GetAsync("/api/search/items?q=COMPUTER");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => i.Name.Contains("Computer"));
    }

    [Fact]
    public async Task Location_Filter_Includes_Nested_Locations()
    {
        // Arrange - Create hierarchy
        var house = await CreateTestLocationAsync("House");
        var kitchen = await CreateTestLocationAsync("Kitchen", house.Id);
        var drawer = await CreateTestLocationAsync("Top Drawer", kitchen.Id);

        // Items at different levels
        await CreateTestItemAsync("Key Rack", house.Id);
        await CreateTestItemAsync("Toaster", kitchen.Id);
        await CreateTestItemAsync("Spoon", drawer.Id);

        // Item in different top-level location
        var garage = await CreateTestLocationAsync("Garage");
        await CreateTestItemAsync("Toolbox", garage.Id);

        // Act - Search within kitchen (should include drawer)
        var response = await _client.GetAsync($"/api/search/items?locationId={kitchen.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "Toaster");
        result.Items.Should().Contain(i => i.Name == "Spoon");
        result.Items.Should().NotContain(i => i.Name == "Key Rack");
        result.Items.Should().NotContain(i => i.Name == "Toolbox");
    }

    [Fact]
    public async Task Combined_Query_And_Location_Filter()
    {
        // Arrange
        var kitchen = await CreateTestLocationAsync("Kitchen");
        var garage = await CreateTestLocationAsync("Garage");

        await CreateTestItemAsync("Kitchen Knife", kitchen.Id);
        await CreateTestItemAsync("Kitchen Towel", kitchen.Id);
        await CreateTestItemAsync("Utility Knife", garage.Id);

        // Act - Search for "knife" only in kitchen
        var response = await _client.GetAsync($"/api/search/items?q=knife&locationId={kitchen.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Kitchen Knife");
    }

    [Fact]
    public async Task Empty_Results_When_No_Matches()
    {
        // Arrange
        var location = await CreateTestLocationAsync("Storage");
        await CreateTestItemAsync("Hammer", location.Id);
        await CreateTestItemAsync("Nails", location.Id);

        // Act
        var response = await _client.GetAsync("/api/search/items?q=xyz123nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Pagination_Works_Correctly()
    {
        // Arrange
        var location = await CreateTestLocationAsync("Inventory");
        for (int i = 1; i <= 15; i++)
        {
            await CreateTestItemAsync($"Item {i:D2}", location.Id);
        }

        // Act - Get pages
        var page1Response = await _client.GetAsync("/api/search/items?limit=5&offset=0");
        var page2Response = await _client.GetAsync("/api/search/items?limit=5&offset=5");
        var page3Response = await _client.GetAsync("/api/search/items?limit=5&offset=10");
        var page4Response = await _client.GetAsync("/api/search/items?limit=5&offset=15");

        // Assert
        var page1 = await page1Response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        var page3 = await page3Response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        var page4 = await page4Response.Content.ReadFromJsonAsync<SearchResultsResponse>();

        page1.Should().NotBeNull();
        page1!.Items.Should().HaveCount(5);
        page1.Total.Should().Be(15);
        page1.HasMore.Should().BeTrue();

        page2.Should().NotBeNull();
        page2!.Items.Should().HaveCount(5);
        page2.Total.Should().Be(15);
        page2.HasMore.Should().BeTrue();

        page3.Should().NotBeNull();
        page3!.Items.Should().HaveCount(5);
        page3.Total.Should().Be(15);
        page3.HasMore.Should().BeFalse();

        page4.Should().NotBeNull();
        page4!.Items.Should().BeEmpty();
        page4.Total.Should().Be(15);
        page4.HasMore.Should().BeFalse();

        // Verify no overlap between pages
        var allNames = page1.Items.Select(i => i.Name)
            .Concat(page2.Items.Select(i => i.Name))
            .Concat(page3.Items.Select(i => i.Name))
            .ToList();
        allNames.Should().OnlyHaveUniqueItems();
        allNames.Should().HaveCount(15);
    }

    [Fact]
    public async Task Search_Returns_Full_Location_Path()
    {
        // Arrange
        var level1 = await CreateTestLocationAsync("Building A");
        var level2 = await CreateTestLocationAsync("Floor 2", level1.Id);
        var level3 = await CreateTestLocationAsync("Room 201", level2.Id);
        var level4 = await CreateTestLocationAsync("Cabinet", level3.Id);

        await CreateTestItemAsync("Important Documents", level4.Id);

        // Act
        var response = await _client.GetAsync("/api/search/items?q=documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].LocationPath.Should().Equal("Building A", "Floor 2", "Room 201", "Cabinet");
    }

    [Fact]
    public async Task Search_Top_Level_Location_Includes_All_Descendants()
    {
        // Arrange - Deep hierarchy
        var root = await CreateTestLocationAsync("Root");
        var child1 = await CreateTestLocationAsync("Child1", root.Id);
        var child2 = await CreateTestLocationAsync("Child2", root.Id);
        var grandchild = await CreateTestLocationAsync("Grandchild", child1.Id);

        await CreateTestItemAsync("Item at Root", root.Id);
        await CreateTestItemAsync("Item at Child1", child1.Id);
        await CreateTestItemAsync("Item at Child2", child2.Id);
        await CreateTestItemAsync("Item at Grandchild", grandchild.Id);

        // Separate location
        var other = await CreateTestLocationAsync("Other");
        await CreateTestItemAsync("Item at Other", other.Id);

        // Act - Search from root
        var response = await _client.GetAsync($"/api/search/items?locationId={root.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(4);
        result.Items.Should().Contain(i => i.Name == "Item at Root");
        result.Items.Should().Contain(i => i.Name == "Item at Child1");
        result.Items.Should().Contain(i => i.Name == "Item at Child2");
        result.Items.Should().Contain(i => i.Name == "Item at Grandchild");
        result.Items.Should().NotContain(i => i.Name == "Item at Other");
    }

    [Fact]
    public async Task Search_Results_Are_Ordered_By_Name()
    {
        // Arrange
        var location = await CreateTestLocationAsync("Storage");
        await CreateTestItemAsync("Zebra", location.Id);
        await CreateTestItemAsync("Apple", location.Id);
        await CreateTestItemAsync("Mango", location.Id);

        // Act
        var response = await _client.GetAsync("/api/search/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Apple");
        result.Items[1].Name.Should().Be("Mango");
        result.Items[2].Name.Should().Be("Zebra");
    }

    private class ItemCreatedResponse
    {
        public Guid Id { get; set; }
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
