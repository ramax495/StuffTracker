using StuffTracker.Api.Domain;
using StuffTracker.Api.Infrastructure.Persistence.Repositories;

namespace StuffTracker.Api.Infrastructure.Telegram;

/// <summary>
/// Middleware that ensures the authenticated Telegram user exists in the database.
/// Creates the user if they don't exist, or updates LastSeenAt if they do.
/// </summary>
public class UserSyncMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserSyncMiddleware> _logger;

    public UserSyncMiddleware(RequestDelegate next, ILogger<UserSyncMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        // Only sync if user is authenticated with Telegram
        var telegramUser = context.GetTelegramUser();
        if (telegramUser != null)
        {
            try
            {
                var user = new User
                {
                    TelegramId = telegramUser.Id,
                    FirstName = telegramUser.FirstName,
                    LastName = telegramUser.LastName,
                    Username = telegramUser.Username,
                    LanguageCode = telegramUser.LanguageCode
                };

                await userRepository.UpsertAsync(user, context.RequestAborted);
                _logger.LogDebug("User {TelegramId} synced to database", telegramUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync user {TelegramId} to database", telegramUser.Id);
                throw;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for UserSyncMiddleware.
/// </summary>
public static class UserSyncMiddlewareExtensions
{
    /// <summary>
    /// Adds the user sync middleware to the pipeline.
    /// Must be called after UseAuthentication().
    /// </summary>
    public static IApplicationBuilder UseUserSync(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserSyncMiddleware>();
    }
}
