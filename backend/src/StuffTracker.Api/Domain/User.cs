namespace StuffTracker.Api.Domain;

/// <summary>
/// Represents a Telegram user who has accessed the Mini App.
/// TelegramId is used as the primary key (from Telegram initData).
/// </summary>
public class User
{
    /// <summary>
    /// Telegram user ID (from initData). Primary key.
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
    /// First access timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTime LastSeenAt { get; set; }

    // Navigation properties
    public ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
