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
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Contract.Locations;

public class MoveLocationTests
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

    private async Task<LocationResponse> CreateTestLocationAsync(HttpClient client, string name, Guid? parentId = null)
    {
        var request = parentId.HasValue
            ? new { Name = name, ParentId = parentId.Value }
            : (object)new { Name = name };
        var response = await client.PostAsJsonAsync("/api/locations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    [Fact]
    public async Task MoveLocation_ToValidParent_Returns200()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var parentA = await CreateTestLocationAsync(client, "Parent A");
        var parentB = await CreateTestLocationAsync(client, "Parent B");
        var child = await CreateTestLocationAsync(client, "Child", parentA.Id);

        var moveRequest = new { ParentId = parentB.Id };

        // Act
        var response = await client.PostAsJsonAsync($"/api/locations/{child.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var movedLocation = await response.Content.ReadFromJsonAsync<LocationResponse>();
        movedLocation.Should().NotBeNull();
        movedLocation!.Id.Should().Be(child.Id);
        movedLocation.Name.Should().Be("Child");
        movedLocation.ParentId.Should().Be(parentB.Id);
        movedLocation.Depth.Should().Be(1);
        movedLocation.Breadcrumbs.Should().Equal("Parent B", "Child");
        movedLocation.UpdatedAt.Should().BeOnOrAfter(child.UpdatedAt);
    }

    [Fact]
    public async Task MoveLocation_ToRoot_Returns200()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var parent = await CreateTestLocationAsync(client, "Parent");
        var child = await CreateTestLocationAsync(client, "Child", parent.Id);

        // Verify initial state
        child.ParentId.Should().Be(parent.Id);
        child.Depth.Should().Be(1);

        var moveRequest = new { ParentId = (Guid?)null };

        // Act
        var response = await client.PostAsJsonAsync($"/api/locations/{child.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var movedLocation = await response.Content.ReadFromJsonAsync<LocationResponse>();
        movedLocation.Should().NotBeNull();
        movedLocation!.Id.Should().Be(child.Id);
        movedLocation.ParentId.Should().BeNull();
        movedLocation.Depth.Should().Be(0);
        movedLocation.Breadcrumbs.Should().Equal("Child");
    }

    [Fact]
    public async Task MoveLocation_ToSelf_Returns400()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client, "Self Location");

        var moveRequest = new { ParentId = location.Id };

        // Act
        var response = await client.PostAsJsonAsync($"/api/locations/{location.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MoveLocation_ToOwnDescendant_Returns400()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        // Create hierarchy: Root -> Child -> Grandchild
        var root = await CreateTestLocationAsync(client, "Root");
        var child = await CreateTestLocationAsync(client, "Child", root.Id);
        var grandchild = await CreateTestLocationAsync(client, "Grandchild", child.Id);

        // Try to move root into its own grandchild (would create cycle)
        var moveRequest = new { ParentId = grandchild.Id };

        // Act
        var response = await client.PostAsJsonAsync($"/api/locations/{root.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MoveLocation_NonExistentLocation_Returns404()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var targetParent = await CreateTestLocationAsync(client, "Target Parent");
        var nonExistentId = Guid.NewGuid();

        var moveRequest = new { ParentId = targetParent.Id };

        // Act
        var response = await client.PostAsJsonAsync($"/api/locations/{nonExistentId}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveLocation_NonExistentTargetParent_Returns404()
    {
        // Arrange
        var (client, factory) = CreateAuthenticatedClientAndFactory();
        using var _ = factory;

        var location = await CreateTestLocationAsync(client, "Location to Move");
        var nonExistentParentId = Guid.NewGuid();

        var moveRequest = new { ParentId = nonExistentParentId };

        // Act
        var response = await client.PostAsJsonAsync($"/api/locations/{location.Id}/move", moveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveLocation_OtherUserLocation_Returns404()
    {
        // Arrange - Create location as primary user
        var (primaryClient, primaryFactory) = CreateAuthenticatedClientAndFactory(TestUserId);
        using var _1 = primaryFactory;

        var userALocation = await CreateTestLocationAsync(primaryClient, "User A Location");

        // Create client for other user
        var (otherClient, otherFactory) = CreateAuthenticatedClientAndFactory(OtherUserId);
        using var _2 = otherFactory;

        // Create a location as other user to use as target
        var userBTarget = await CreateTestLocationAsync(otherClient, "User B Target");

        // Act - Try to move User A's location as User B
        var moveRequest = new { ParentId = userBTarget.Id };
        var response = await otherClient.PostAsJsonAsync($"/api/locations/{userALocation.Id}/move", moveRequest);

        // Assert - Should get 404 (location not found for this user)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveLocation_NoAuth_Returns401()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        // Note: Not adding auth header

        var moveRequest = new { ParentId = Guid.NewGuid() };

        // Act
        var response = await client.PostAsJsonAsync($"/api/locations/{Guid.NewGuid()}/move", moveRequest);

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
