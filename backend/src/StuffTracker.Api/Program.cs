using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using NSwag;
using StuffTracker.Api.Common;
using StuffTracker.Api.Infrastructure.Persistence;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;
using StuffTracker.Api.Infrastructure.Telegram;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Configure Telegram settings
var telegramSettings = builder.Configuration
    .GetSection(TelegramSettings.SectionName)
    .Get<TelegramSettings>() ?? new TelegramSettings();

// Dev mode is only allowed in Development environment
var isDevModeEnabled = telegramSettings.DevMode && builder.Environment.IsDevelopment();

// Register Telegram validation service
if (isDevModeEnabled)
{
    builder.Services.AddSingleton(telegramSettings);
    builder.Services.AddSingleton<ITelegramValidationService, DevTelegramValidationService>();
}
else
{
    if (string.IsNullOrEmpty(telegramSettings.BotToken))
    {
        throw new InvalidOperationException("Telegram:BotToken configuration is required");
    }
    builder.Services.AddSingleton<ITelegramValidationService>(
        new TelegramValidationService(telegramSettings.BotToken));
}

// Add Authentication with Telegram
builder.Services.AddAuthentication(TelegramAuthenticationHandler.SchemeName)
    .AddTelegramAuthentication(options =>
    {
        options.DevMode = isDevModeEnabled;
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add FastEndpoints
builder.Services.AddFastEndpoints(o =>
{
    o.Assemblies = new[] { typeof(Program).Assembly };
});

// Add Swagger documentation with Telegram authentication
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "StuffTracker API";
        s.Version = "v1";
        s.Description = "API for StuffTracker - Telegram Mini App for tracking personal items and storage locations. " +
            "All endpoints require Telegram Mini App authentication via the X-Telegram-Init-Data header.";

        // Add Telegram authentication security definition
        s.AddSecurity("TelegramAuth", new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.ApiKey,
            Name = "X-Telegram-Init-Data",
            In = OpenApiSecurityApiKeyLocation.Header,
            Description = "Telegram Mini App initData string for authentication. " +
                "This is the raw initData provided by the Telegram WebApp SDK."
        });
    };

    // Apply security requirement globally to all endpoints
    o.EnableJWTBearerAuth = false;
});

// Add CORS for Telegram Mini App and frontend development
builder.Services.AddCors(options =>
{
    options.AddPolicy("TelegramMiniApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    // Keep permissive policy for production Telegram Mini App context
    options.AddPolicy("TelegramProduction", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply pending migrations automatically (skip for InMemory DB used in tests)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Global exception handler middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception occurred");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(ApiErrorResponse.InternalError(
            app.Environment.IsDevelopment() ? ex.Message : "An unexpected error occurred"));
    }
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseCors("TelegramMiniApp");
}
else
{
    app.UseCors("TelegramProduction");
}

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseUserSync(); // Ensure authenticated user exists in database
app.UseAuthorization();

// Use FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
});

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
