namespace StuffTracker.Api.Infrastructure.Telegram;

/// <summary>
/// Configuration settings for Telegram integration.
/// </summary>
public class TelegramSettings
{
    public const string SectionName = "Telegram";

    /// <summary>
    /// Telegram Bot Token from @BotFather.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// URL where the Mini App is hosted.
    /// </summary>
    public string? WebAppUrl { get; set; }

    /// <summary>
    /// Enable development mode to bypass Telegram authentication.
    /// Only works when ASPNETCORE_ENVIRONMENT=Development.
    /// </summary>
    public bool DevMode { get; set; }

    /// <summary>
    /// Telegram user ID to use in dev mode.
    /// </summary>
    public long DevUserId { get; set; } = 123456789;

    /// <summary>
    /// First name of the dev user.
    /// </summary>
    public string DevUserFirstName { get; set; } = "Developer";

    /// <summary>
    /// Last name of the dev user (optional).
    /// </summary>
    public string? DevUserLastName { get; set; }

    /// <summary>
    /// Username of the dev user (optional).
    /// </summary>
    public string? DevUserUsername { get; set; } = "dev_user";
}
