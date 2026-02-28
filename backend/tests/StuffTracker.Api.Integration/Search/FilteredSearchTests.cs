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
/// Integration tests for filtered search functionality with nested location hierarchies.
/// Tests verify that location filters correctly include all nested locations using recursive CTE.
/// </summary>
[Trait("Category", "Integration")]
public class FilteredSearchTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private FilteredSearchTestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private const long TestUserId = 888777666;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgres.StartAsync();

        _factory = new FilteredSearchTestWebApplicationFactory(_postgres.GetConnectionString());

        var telegramUser = new TelegramUser
        {
            Id = TestUserId,
            FirstName = "FilterTest",
            LastName = "User",
            Username = "filtertestuser",
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

    #region Test 1: Filter by top-level location includes all nested items

    [Fact]
    public async Task FilterByTopLevelLocation_IncludesAllNestedItems()
    {
        // Arrange - Create hierarchy: Home > Living Room > Bookshelf
        var home = await CreateTestLocationAsync("Home");
        var livingRoom = await CreateTestLocationAsync("Living Room", home.Id);
        var bookshelf = await CreateTestLocationAsync("Bookshelf", livingRoom.Id);

        // Add items at each level
        await CreateTestItemAsync("Welcome Mat", home.Id);
        await CreateTestItemAsync("Sofa", livingRoom.Id);
        await CreateTestItemAsync("Coffee Table", livingRoom.Id);
        await CreateTestItemAsync("Novel Collection", bookshelf.Id);
        await CreateTestItemAsync("Encyclopedia Set", bookshelf.Id);

        // Create another top-level location with items (should NOT be included)
        var garage = await CreateTestLocationAsync("Garage");
        await CreateTestItemAsync("Toolbox", garage.Id);

        // Act - Filter by "Home" should return all items in the Home subtree
        var response = await _client.GetAsync($"/api/search/items?locationId={home.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(5);
        result.Items.Should().Contain(i => i.Name == "Welcome Mat");
        result.Items.Should().Contain(i => i.Name == "Sofa");
        result.Items.Should().Contain(i => i.Name == "Coffee Table");
        result.Items.Should().Contain(i => i.Name == "Novel Collection");
        result.Items.Should().Contain(i => i.Name == "Encyclopedia Set");
        result.Items.Should().NotContain(i => i.Name == "Toolbox");
    }

    #endregion

    #region Test 2: Filter by mid-level location includes only that subtree

    [Fact]
    public async Task FilterByMidLevelLocation_IncludesOnlySubtree()
    {
        // Arrange - Create hierarchy: Office > Desk > Drawer and Office > Filing Cabinet
        var office = await CreateTestLocationAsync("Office");
        var desk = await CreateTestLocationAsync("Desk", office.Id);
        var drawer = await CreateTestLocationAsync("Drawer", desk.Id);
        var filingCabinet = await CreateTestLocationAsync("Filing Cabinet", office.Id);

        // Add items at each level
        await CreateTestItemAsync("Office Plant", office.Id);
        await CreateTestItemAsync("Computer Monitor", desk.Id);
        await CreateTestItemAsync("Keyboard", desk.Id);
        await CreateTestItemAsync("Pens", drawer.Id);
        await CreateTestItemAsync("Stapler", drawer.Id);
        await CreateTestItemAsync("Tax Documents", filingCabinet.Id);

        // Act - Filter by "Desk" should return Desk + Drawer items only
        var response = await _client.GetAsync($"/api/search/items?locationId={desk.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(4);
        result.Items.Should().Contain(i => i.Name == "Computer Monitor");
        result.Items.Should().Contain(i => i.Name == "Keyboard");
        result.Items.Should().Contain(i => i.Name == "Pens");
        result.Items.Should().Contain(i => i.Name == "Stapler");
        // Should NOT include items from sibling or parent locations
        result.Items.Should().NotContain(i => i.Name == "Office Plant");
        result.Items.Should().NotContain(i => i.Name == "Tax Documents");
    }

    [Fact]
    public async Task FilterByMidLevelLocation_ExcludesOtherBranches()
    {
        // Arrange - Create two separate branches under same parent
        var warehouse = await CreateTestLocationAsync("Warehouse");
        var sectionA = await CreateTestLocationAsync("Section A", warehouse.Id);
        var shelfA1 = await CreateTestLocationAsync("Shelf A1", sectionA.Id);
        var sectionB = await CreateTestLocationAsync("Section B", warehouse.Id);
        var shelfB1 = await CreateTestLocationAsync("Shelf B1", sectionB.Id);

        await CreateTestItemAsync("Item in Section A", sectionA.Id);
        await CreateTestItemAsync("Item in Shelf A1", shelfA1.Id);
        await CreateTestItemAsync("Item in Section B", sectionB.Id);
        await CreateTestItemAsync("Item in Shelf B1", shelfB1.Id);
        await CreateTestItemAsync("Item in Warehouse", warehouse.Id);

        // Act - Filter by "Section A"
        var response = await _client.GetAsync($"/api/search/items?locationId={sectionA.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "Item in Section A");
        result.Items.Should().Contain(i => i.Name == "Item in Shelf A1");
        // Should NOT include items from Section B branch or parent
        result.Items.Should().NotContain(i => i.Name == "Item in Section B");
        result.Items.Should().NotContain(i => i.Name == "Item in Shelf B1");
        result.Items.Should().NotContain(i => i.Name == "Item in Warehouse");
    }

    #endregion

    #region Test 3: Filter by leaf location returns only direct items

    [Fact]
    public async Task FilterByLeafLocation_ReturnsOnlyDirectItems()
    {
        // Arrange - Create hierarchy: Basement > Storage Room > Box
        var basement = await CreateTestLocationAsync("Basement");
        var storageRoom = await CreateTestLocationAsync("Storage Room", basement.Id);
        var box = await CreateTestLocationAsync("Box", storageRoom.Id);

        await CreateTestItemAsync("Water Heater", basement.Id);
        await CreateTestItemAsync("Old Furniture", storageRoom.Id);
        await CreateTestItemAsync("Holiday Decorations", box.Id);
        await CreateTestItemAsync("Photo Albums", box.Id);

        // Act - Filter by "Box" (leaf location)
        var response = await _client.GetAsync($"/api/search/items?locationId={box.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "Holiday Decorations");
        result.Items.Should().Contain(i => i.Name == "Photo Albums");
        // Should NOT include items from parent locations
        result.Items.Should().NotContain(i => i.Name == "Water Heater");
        result.Items.Should().NotContain(i => i.Name == "Old Furniture");
    }

    [Fact]
    public async Task FilterByLeafLocation_WithNoChildren_ReturnsOnlyDirectItems()
    {
        // Arrange - Create a location with no children
        var shed = await CreateTestLocationAsync("Shed");
        var toolRack = await CreateTestLocationAsync("Tool Rack", shed.Id);

        await CreateTestItemAsync("Lawnmower", shed.Id);
        await CreateTestItemAsync("Hammer", toolRack.Id);
        await CreateTestItemAsync("Screwdriver", toolRack.Id);
        await CreateTestItemAsync("Wrench", toolRack.Id);

        // Act - Filter by leaf location "Tool Rack"
        var response = await _client.GetAsync($"/api/search/items?locationId={toolRack.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(i =>
            i.Name == "Hammer" || i.Name == "Screwdriver" || i.Name == "Wrench");
    }

    #endregion

    #region Test 4: Combined query + location filter

    [Fact]
    public async Task CombinedQueryAndLocationFilter_ReturnsMatchingItemsInSubtree()
    {
        // Arrange
        var library = await CreateTestLocationAsync("Library");
        var fictionSection = await CreateTestLocationAsync("Fiction Section", library.Id);
        var nonFictionSection = await CreateTestLocationAsync("Non-Fiction Section", library.Id);

        await CreateTestItemAsync("Mystery Book", fictionSection.Id);
        await CreateTestItemAsync("Romance Book", fictionSection.Id);
        await CreateTestItemAsync("History Book", nonFictionSection.Id);
        await CreateTestItemAsync("Science Book", nonFictionSection.Id);
        await CreateTestItemAsync("Book Lamp", library.Id); // Has "Book" in name but at parent level

        // Act - Search for "book" filtered by "Fiction Section"
        var response = await _client.GetAsync(
            $"/api/search/items?q=book&locationId={fictionSection.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "Mystery Book");
        result.Items.Should().Contain(i => i.Name == "Romance Book");
        // Should NOT include books from other sections or parent
        result.Items.Should().NotContain(i => i.Name == "History Book");
        result.Items.Should().NotContain(i => i.Name == "Science Book");
        result.Items.Should().NotContain(i => i.Name == "Book Lamp");
    }

    [Fact]
    public async Task CombinedQueryAndLocationFilter_WithDeepHierarchy()
    {
        // Arrange - Deep hierarchy with searchable items
        var building = await CreateTestLocationAsync("Building A");
        var floor = await CreateTestLocationAsync("Floor 2", building.Id);
        var room = await CreateTestLocationAsync("Room 201", floor.Id);
        var cabinet = await CreateTestLocationAsync("Cabinet", room.Id);

        await CreateTestItemAsync("Red Folder", building.Id);
        await CreateTestItemAsync("Blue Folder", floor.Id);
        await CreateTestItemAsync("Green Folder", room.Id);
        await CreateTestItemAsync("Yellow Folder", cabinet.Id);
        await CreateTestItemAsync("Important Binder", cabinet.Id); // Does not match query

        // Create another building with similar items
        var buildingB = await CreateTestLocationAsync("Building B");
        await CreateTestItemAsync("Purple Folder", buildingB.Id);

        // Act - Search for "folder" filtered by "Floor 2"
        var response = await _client.GetAsync(
            $"/api/search/items?q=folder&locationId={floor.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items.Should().Contain(i => i.Name == "Blue Folder");
        result.Items.Should().Contain(i => i.Name == "Green Folder");
        result.Items.Should().Contain(i => i.Name == "Yellow Folder");
        // Should NOT include items outside the subtree or non-matching items
        result.Items.Should().NotContain(i => i.Name == "Red Folder");
        result.Items.Should().NotContain(i => i.Name == "Purple Folder");
        result.Items.Should().NotContain(i => i.Name == "Important Binder");
    }

    [Fact]
    public async Task CombinedQueryAndLocationFilter_CaseInsensitive()
    {
        // Arrange
        var kitchen = await CreateTestLocationAsync("Kitchen");
        var pantry = await CreateTestLocationAsync("Pantry", kitchen.Id);

        await CreateTestItemAsync("COFFEE BEANS", pantry.Id);
        await CreateTestItemAsync("coffee maker", kitchen.Id);
        await CreateTestItemAsync("Coffee Mug", pantry.Id);
        await CreateTestItemAsync("Tea Kettle", pantry.Id);

        // Act - Search for "COFFEE" (uppercase) in pantry
        var response = await _client.GetAsync(
            $"/api/search/items?q=COFFEE&locationId={pantry.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "COFFEE BEANS");
        result.Items.Should().Contain(i => i.Name == "Coffee Mug");
    }

    #endregion

    #region Test 5: Filter with empty results

    [Fact]
    public async Task FilterByLocationWithNoItems_ReturnsEmptyResults()
    {
        // Arrange - Create location with no items
        var emptyWarehouse = await CreateTestLocationAsync("Empty Warehouse");
        var emptyRoom = await CreateTestLocationAsync("Empty Room", emptyWarehouse.Id);

        // Create items in a different location
        var fullWarehouse = await CreateTestLocationAsync("Full Warehouse");
        await CreateTestItemAsync("Box of Supplies", fullWarehouse.Id);

        // Act - Filter by empty location
        var response = await _client.GetAsync($"/api/search/items?locationId={emptyWarehouse.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task FilterByLocationWithItemsButNoQueryMatch_ReturnsEmptyResults()
    {
        // Arrange
        var closet = await CreateTestLocationAsync("Closet");
        await CreateTestItemAsync("Winter Coat", closet.Id);
        await CreateTestItemAsync("Summer Dress", closet.Id);

        // Act - Search for non-matching query in location that has items
        var response = await _client.GetAsync(
            $"/api/search/items?q=xyznonexistent&locationId={closet.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task FilterByLeafLocationWithNoItems_ReturnsEmptyResults()
    {
        // Arrange - Create hierarchy where leaf has no items
        var attic = await CreateTestLocationAsync("Attic");
        var emptyCorner = await CreateTestLocationAsync("Empty Corner", attic.Id);
        await CreateTestItemAsync("Old Trunk", attic.Id); // Item in parent, not in leaf

        // Act
        var response = await _client.GetAsync($"/api/search/items?locationId={emptyCorner.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    #endregion

    #region Test 6: Invalid location ID returns 404

    [Fact]
    public async Task FilterByNonExistentLocationId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/search/items?locationId={nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FilterByInvalidGuidFormat_Returns400()
    {
        // Act - Pass invalid GUID format
        var response = await _client.GetAsync("/api/search/items?locationId=not-a-valid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FilterByEmptyGuid_Returns404()
    {
        // Act - Pass empty GUID
        var response = await _client.GetAsync($"/api/search/items?locationId={Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Additional edge cases

    [Fact]
    public async Task FilterWithPagination_WorksCorrectly()
    {
        // Arrange - Create location with many items
        var storage = await CreateTestLocationAsync("Large Storage");
        for (int i = 1; i <= 15; i++)
        {
            await CreateTestItemAsync($"Storage Item {i:D2}", storage.Id);
        }

        // Act - Get paginated results with location filter
        var page1Response = await _client.GetAsync(
            $"/api/search/items?locationId={storage.Id}&limit=5&offset=0");
        var page2Response = await _client.GetAsync(
            $"/api/search/items?locationId={storage.Id}&limit=5&offset=5");
        var page3Response = await _client.GetAsync(
            $"/api/search/items?locationId={storage.Id}&limit=5&offset=10");

        // Assert
        var page1 = await page1Response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        var page3 = await page3Response.Content.ReadFromJsonAsync<SearchResultsResponse>();

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

        // Verify no duplicates across pages
        var allNames = page1.Items.Select(i => i.Name)
            .Concat(page2.Items.Select(i => i.Name))
            .Concat(page3.Items.Select(i => i.Name))
            .ToList();
        allNames.Should().OnlyHaveUniqueItems();
        allNames.Should().HaveCount(15);
    }

    [Fact]
    public async Task FilterByDeeplyNestedLocation_WorksCorrectly()
    {
        // Arrange - Create deep hierarchy (5 levels)
        var level1 = await CreateTestLocationAsync("Level 1");
        var level2 = await CreateTestLocationAsync("Level 2", level1.Id);
        var level3 = await CreateTestLocationAsync("Level 3", level2.Id);
        var level4 = await CreateTestLocationAsync("Level 4", level3.Id);
        var level5 = await CreateTestLocationAsync("Level 5", level4.Id);

        await CreateTestItemAsync("Item at Level 1", level1.Id);
        await CreateTestItemAsync("Item at Level 3", level3.Id);
        await CreateTestItemAsync("Item at Level 5", level5.Id);

        // Act - Filter from Level 3 (should include Level 3, 4, 5)
        var response = await _client.GetAsync($"/api/search/items?locationId={level3.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.Name == "Item at Level 3");
        result.Items.Should().Contain(i => i.Name == "Item at Level 5");
        result.Items.Should().NotContain(i => i.Name == "Item at Level 1");
    }

    [Fact]
    public async Task FilteredSearch_ReturnsCorrectLocationPath()
    {
        // Arrange
        var house = await CreateTestLocationAsync("My House");
        var bedroom = await CreateTestLocationAsync("Master Bedroom", house.Id);
        var closet = await CreateTestLocationAsync("Walk-in Closet", bedroom.Id);

        await CreateTestItemAsync("Designer Shoes", closet.Id);

        // Act
        var response = await _client.GetAsync($"/api/search/items?locationId={house.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultsResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Designer Shoes");
        result.Items[0].LocationPath.Should().Equal("My House", "Master Bedroom", "Walk-in Closet");
    }

    #endregion

    private class ItemCreatedResponse
    {
        public Guid Id { get; set; }
    }

    public class FilteredSearchTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public FilteredSearchTestWebApplicationFactory(string connectionString)
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
