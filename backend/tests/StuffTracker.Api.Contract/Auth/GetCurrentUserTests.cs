using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StuffTracker.Api.Features.Auth.GetCurrentUser;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Contract.Auth;

public class GetCurrentUserTests : IClassFixture<GetCurrentUserTests.TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GetCurrentUserTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Returns_200_With_User_Data_When_Valid_Auth()
    {
        // Arrange
        var telegramUser = new TelegramUser
        {
            Id = 123456789,
            FirstName = "John",
            LastName = "Doe",
            Username = "johndoe",
            LanguageCode = "en"
        };

        var mockValidationService = Substitute.For<ITelegramValidationService>();
        mockValidationService.Validate(Arg.Any<string>())
            .Returns(TelegramValidationResult.Success(telegramUser));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the validation service with our mock
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

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse!.TelegramId.Should().Be(123456789);
        userResponse.FirstName.Should().Be("John");
        userResponse.LastName.Should().Be("Doe");
        userResponse.Username.Should().Be("johndoe");
        userResponse.LanguageCode.Should().Be("en");
        userResponse.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Returns_401_When_Missing_Auth_Header()
    {
        // Arrange
        var client = _factory.CreateClient();
        // Note: Not adding X-Telegram-Init-Data header

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_401_When_Invalid_Auth()
    {
        // Arrange
        var mockValidationService = Substitute.For<ITelegramValidationService>();
        mockValidationService.Validate(Arg.Any<string>())
            .Returns(TelegramValidationResult.Failed("Invalid hash"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the validation service with our mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ITelegramValidationService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockValidationService);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add(TelegramAuthenticationHandler.HeaderName, "invalid_init_data");

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Add test configuration before Program.cs runs
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
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
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
