using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StuffTracker.Api.Features.Auth.GetCurrentUser;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Contract.Auth;

public class DevModeAuthTests : IClassFixture<DevModeAuthTests.DevModeWebApplicationFactory>
{
    private readonly DevModeWebApplicationFactory _factory;

    public DevModeAuthTests(DevModeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DevMode_Returns_200_Without_InitData_Header()
    {
        // Arrange — client with NO X-Telegram-Init-Data header
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert — DevMode should authenticate as the default dev user (123456789)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse!.TelegramId.Should().Be(123456789);
        userResponse.FirstName.Should().Be("Developer");
    }

    [Fact]
    public async Task DevMode_Returns_200_For_Protected_Endpoint_Without_Header()
    {
        // Arrange — client with NO auth header hits a data endpoint
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/locations");

        // Assert — should not be 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// WebApplicationFactory configured with Telegram:DevMode=true.
    /// Uses DevTelegramValidationService instead of mock — tests the real dev auth path.
    /// </summary>
    public class DevModeWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:DevMode"] = "true",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace DB with in-memory
                var dbContextDescriptors = services.Where(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                         d.ServiceType == typeof(AppDbContext) ||
                         d.ServiceType == typeof(DbContextOptions)).ToList();
                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });

                // NOTE: Do NOT replace ITelegramValidationService here.
                // We want Program.cs to register DevTelegramValidationService
                // because DevMode=true + Development environment.
            });

            builder.UseEnvironment("Development");
        }
    }
}
