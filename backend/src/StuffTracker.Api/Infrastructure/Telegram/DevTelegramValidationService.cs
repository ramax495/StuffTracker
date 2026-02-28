namespace StuffTracker.Api.Infrastructure.Telegram;

/// <summary>
/// Development-only validation service that bypasses Telegram authentication.
/// Always returns a configured test user without validating initData.
/// WARNING: Only use in development environment!
/// </summary>
public class DevTelegramValidationService : ITelegramValidationService
{
    private readonly TelegramUser _devUser;
    private readonly ILogger<DevTelegramValidationService> _logger;

    public DevTelegramValidationService(TelegramSettings settings, ILogger<DevTelegramValidationService> logger)
    {
        _logger = logger;
        _devUser = new TelegramUser
        {
            Id = settings.DevUserId,
            FirstName = settings.DevUserFirstName,
            LastName = settings.DevUserLastName,
            Username = settings.DevUserUsername
        };

        _logger.LogWarning(
            "⚠️  DEV MODE ENABLED: All requests authenticated as user {UserId} ({FirstName}). " +
            "DO NOT use in production!",
            _devUser.Id,
            _devUser.FirstName);
    }

    public TelegramValidationResult Validate(string initData)
    {
        // In dev mode, we accept any request (even without initData)
        // and return the configured dev user
        _logger.LogDebug(
            "Dev mode: Bypassing Telegram validation, authenticating as user {UserId}",
            _devUser.Id);

        return TelegramValidationResult.Success(_devUser);
    }
}
