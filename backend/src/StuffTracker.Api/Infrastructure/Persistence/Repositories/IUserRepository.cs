using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for User operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their Telegram ID.
    /// </summary>
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a user based on Telegram data.
    /// If the user exists, updates their info and LastSeenAt.
    /// If the user doesn't exist, creates a new user.
    /// </summary>
    Task<User> UpsertAsync(User user, CancellationToken ct = default);
}
