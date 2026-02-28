using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.Common;
using StuffTracker.Api.Domain;
using StuffTracker.Api.Infrastructure.Persistence;

namespace StuffTracker.Api.Features.Auth.GetCurrentUser;

/// <summary>
/// Endpoint for getting the current authenticated user.
/// GET /api/auth/me
/// </summary>
public class GetCurrentUserEndpoint : BaseEndpointWithoutRequest<UserResponse>
{
    private readonly AppDbContext _dbContext;

    public GetCurrentUserEndpoint(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/auth/me");
        Summary(s =>
        {
            s.Summary = "Get current authenticated user";
            s.Description = "Returns the profile information for the currently authenticated Telegram user. Creates user record if it doesn't exist.";
            s.Responses[200] = "Returns the user profile";
            s.Responses[401] = "Invalid or missing Telegram authentication";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var telegramUser = GetTelegramUser();
        if (telegramUser == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.WriteAsJsonAsync(
                ApiErrorResponse.Unauthorized(), ct);
            return;
        }

        var now = DateTime.UtcNow;

        // Try to find existing user
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramUser.Id, ct);

        if (user == null)
        {
            // Create new user
            user = new User
            {
                TelegramId = telegramUser.Id,
                FirstName = telegramUser.FirstName,
                LastName = telegramUser.LastName,
                Username = telegramUser.Username,
                LanguageCode = telegramUser.LanguageCode,
                CreatedAt = now,
                LastSeenAt = now
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(ct);
        }
        else
        {
            // Update existing user's LastSeenAt and profile info
            user.FirstName = telegramUser.FirstName;
            user.LastName = telegramUser.LastName;
            user.Username = telegramUser.Username;
            user.LanguageCode = telegramUser.LanguageCode;
            user.LastSeenAt = now;

            await _dbContext.SaveChangesAsync(ct);
        }

        Response = new UserResponse
        {
            TelegramId = user.TelegramId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            LanguageCode = user.LanguageCode,
            CreatedAt = user.CreatedAt
        };
    }
}
