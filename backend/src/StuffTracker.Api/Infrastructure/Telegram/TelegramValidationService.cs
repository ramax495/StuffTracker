using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace StuffTracker.Api.Infrastructure.Telegram;

/// <summary>
/// Service for validating Telegram Mini App initData.
/// Implements HMAC-SHA256 validation as per Telegram documentation.
/// </summary>
public interface ITelegramValidationService
{
    /// <summary>
    /// Validates the initData string from Telegram WebApp.
    /// </summary>
    /// <param name="initData">The raw initData query string</param>
    /// <returns>Validation result with parsed user data if valid</returns>
    TelegramValidationResult Validate(string initData);
}

public class TelegramValidationService : ITelegramValidationService
{
    private readonly string _botToken;
    private readonly TimeSpan _maxAge;

    public TelegramValidationService(string botToken, TimeSpan? maxAge = null)
    {
        _botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
        _maxAge = maxAge ?? TimeSpan.FromHours(24);
    }

    public TelegramValidationResult Validate(string initData)
    {
        if (string.IsNullOrWhiteSpace(initData))
        {
            return TelegramValidationResult.Failed("initData is empty");
        }

        try
        {
            // Parse the initData as query string
            var parameters = HttpUtility.ParseQueryString(initData);
            var hash = parameters["hash"];

            if (string.IsNullOrEmpty(hash))
            {
                return TelegramValidationResult.Failed("Missing hash parameter");
            }

            // Build data-check-string: sorted key=value pairs joined by \n, excluding hash
            var dataCheckPairs = parameters.AllKeys
                .Where(k => k != "hash" && k != null)
                .OrderBy(k => k, StringComparer.Ordinal)
                .Select(k => $"{k}={parameters[k]}")
                .ToList();

            var dataCheckString = string.Join("\n", dataCheckPairs);

            // Compute secret key: HMAC-SHA256("WebAppData", bot_token)
            var secretKey = ComputeHmacSha256(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(_botToken));

            // Compute hash: HMAC-SHA256(secret_key, data_check_string)
            var computedHashBytes = ComputeHmacSha256(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
            var computedHash = Convert.ToHexString(computedHashBytes).ToLowerInvariant();

            // Compare hashes
            if (!string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                return TelegramValidationResult.Failed("Invalid hash");
            }

            // Validate auth_date is not too old
            var authDateStr = parameters["auth_date"];
            if (!string.IsNullOrEmpty(authDateStr) && long.TryParse(authDateStr, out var authDateUnix))
            {
                var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
                if (DateTimeOffset.UtcNow - authDate > _maxAge)
                {
                    return TelegramValidationResult.Failed("initData is expired");
                }
            }

            // Parse user data
            var userJson = parameters["user"];
            if (string.IsNullOrEmpty(userJson))
            {
                return TelegramValidationResult.Failed("Missing user parameter");
            }

            var user = JsonSerializer.Deserialize<TelegramUser>(userJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (user == null || user.Id == 0)
            {
                return TelegramValidationResult.Failed("Invalid user data");
            }

            return TelegramValidationResult.Success(user);
        }
        catch (Exception ex)
        {
            return TelegramValidationResult.Failed($"Validation error: {ex.Message}");
        }
    }

    private static byte[] ComputeHmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }
}

/// <summary>
/// Result of Telegram initData validation.
/// </summary>
public class TelegramValidationResult
{
    public bool IsValid { get; private init; }
    public TelegramUser? User { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static TelegramValidationResult Success(TelegramUser user) => new()
    {
        IsValid = true,
        User = user
    };

    public static TelegramValidationResult Failed(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Telegram user data from initData.
/// </summary>
public class TelegramUser
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? LanguageCode { get; set; }
    public bool? IsPremium { get; set; }
    public bool? AllowsWriteToPm { get; set; }
}
