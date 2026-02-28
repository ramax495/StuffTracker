using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace StuffTracker.Api.Infrastructure.Telegram;

/// <summary>
/// ASP.NET Core authentication handler for Telegram Mini App initData.
/// </summary>
public class TelegramAuthenticationHandler : AuthenticationHandler<TelegramAuthenticationOptions>
{
    public const string SchemeName = "TelegramAuth";
    public const string HeaderName = "X-Telegram-Init-Data";
    public const string TelegramIdClaimType = "telegram_id";

    private readonly ITelegramValidationService _validationService;

    public TelegramAuthenticationHandler(
        IOptionsMonitor<TelegramAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITelegramValidationService validationService)
        : base(options, logger, encoder)
    {
        _validationService = validationService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for the Telegram init data header
        Request.Headers.TryGetValue(HeaderName, out var initDataValues);
        var initData = initDataValues.FirstOrDefault();

        // In dev mode, allow requests without the header
        if (string.IsNullOrWhiteSpace(initData))
        {
            if (!Options.DevMode)
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing X-Telegram-Init-Data header"));
            }
            // Dev mode: pass empty string to validation service which will return dev user
            initData = string.Empty;
        }

        // Validate the init data (or bypass in dev mode)
        var validationResult = _validationService.Validate(initData);
        if (!validationResult.IsValid)
        {
            Logger.LogWarning("Telegram authentication failed: {Error}", validationResult.ErrorMessage);
            return Task.FromResult(AuthenticateResult.Fail(validationResult.ErrorMessage ?? "Invalid initData"));
        }

        var user = validationResult.User!;

        // Create claims
        var claims = new List<Claim>
        {
            new(TelegramIdClaimType, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.GivenName, user.FirstName)
        };

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        if (!string.IsNullOrEmpty(user.Username))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.Username));
        }

        // Create identity and principal
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        // Store the full user info in HttpContext.Items for later use
        Context.Items["TelegramUser"] = user;

        Logger.LogDebug("Telegram authentication successful for user {TelegramId}", user.Id);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        return Response.WriteAsJsonAsync(new
        {
            type = "error",
            statusCode = 401,
            message = "Invalid or missing Telegram authentication"
        });
    }
}

/// <summary>
/// Options for Telegram authentication.
/// </summary>
public class TelegramAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// When true, allows requests without X-Telegram-Init-Data header in dev mode.
    /// The validation service will provide a dev user instead.
    /// </summary>
    public bool DevMode { get; set; }
}

/// <summary>
/// Extension methods for Telegram authentication.
/// </summary>
public static class TelegramAuthenticationExtensions
{
    public static AuthenticationBuilder AddTelegramAuthentication(
        this AuthenticationBuilder builder,
        Action<TelegramAuthenticationOptions>? configureOptions = null)
    {
        return builder.AddScheme<TelegramAuthenticationOptions, TelegramAuthenticationHandler>(
            TelegramAuthenticationHandler.SchemeName,
            configureOptions ?? (_ => { }));
    }

    /// <summary>
    /// Gets the Telegram user ID from the claims principal.
    /// </summary>
    public static long? GetTelegramId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(TelegramAuthenticationHandler.TelegramIdClaimType);
        if (claim != null && long.TryParse(claim.Value, out var telegramId))
        {
            return telegramId;
        }
        return null;
    }

    /// <summary>
    /// Gets the Telegram user from HttpContext.Items.
    /// </summary>
    public static TelegramUser? GetTelegramUser(this HttpContext context)
    {
        return context.Items["TelegramUser"] as TelegramUser;
    }
}
