namespace StuffTracker.Api.Features.Auth.GetCurrentUser;

/// <summary>
/// Response DTO for the current authenticated user.
/// Per api.yaml UserResponse schema.
/// </summary>
public class UserResponse
{
    /// <summary>
    /// Telegram user ID.
    /// </summary>
    public long TelegramId { get; set; }

    /// <summary>
    /// First name from Telegram profile.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name from Telegram profile (optional).
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Telegram username without @ (optional).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// User's language code (e.g., "ru", "en").
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// First access timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
