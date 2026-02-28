using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.Domain;

namespace StuffTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for User operations.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId, ct);
    }

    /// <inheritdoc />
    public async Task<User> UpsertAsync(User user, CancellationToken ct = default)
    {
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.TelegramId == user.TelegramId, ct);

        if (existingUser == null)
        {
            // Create new user
            user.CreatedAt = DateTime.UtcNow;
            user.LastSeenAt = DateTime.UtcNow;
            _dbContext.Users.Add(user);
        }
        else
        {
            // Update existing user
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Username = user.Username;
            existingUser.LanguageCode = user.LanguageCode;
            existingUser.LastSeenAt = DateTime.UtcNow;
            user = existingUser;
        }

        await _dbContext.SaveChangesAsync(ct);
        return user;
    }
}
