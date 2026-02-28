using FastEndpoints;
using StuffTracker.Api.Infrastructure.Telegram;

namespace StuffTracker.Api.Common;

/// <summary>
/// Base endpoint class with common functionality for all API endpoints.
/// </summary>
public abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Gets the authenticated Telegram user ID.
    /// </summary>
    protected long GetUserId()
    {
        var userId = User.GetTelegramId();
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("User is not authenticated");
        }
        return userId.Value;
    }

    /// <summary>
    /// Gets the Telegram user info from the current request.
    /// </summary>
    protected TelegramUser? GetTelegramUser()
    {
        return HttpContext.GetTelegramUser();
    }

    /// <summary>
    /// Sends a 404 Not Found response.
    /// </summary>
    protected Task SendNotFoundAsync(string message = "Resource not found", CancellationToken ct = default)
    {
        return SendErrorAsync(ApiErrorResponse.NotFound(message), 404, ct);
    }

    /// <summary>
    /// Sends a 400 Bad Request response.
    /// </summary>
    protected Task SendBadRequestAsync(string message = "Bad request", CancellationToken ct = default)
    {
        return SendErrorAsync(ApiErrorResponse.BadRequest(message), 400, ct);
    }

    /// <summary>
    /// Sends a 400 Validation Error response.
    /// </summary>
    protected Task SendValidationErrorAsync(Dictionary<string, string[]> errors, string message = "Validation failed", CancellationToken ct = default)
    {
        return SendErrorAsync(ApiErrorResponse.ValidationError(message, errors), 400, ct);
    }

    /// <summary>
    /// Sends a 409 Conflict response.
    /// </summary>
    protected Task SendConflictAsync(string message = "Conflict", CancellationToken ct = default)
    {
        return SendErrorAsync(ApiErrorResponse.Conflict(message), 409, ct);
    }

    private Task SendErrorAsync(ApiErrorResponse error, int statusCode, CancellationToken ct)
    {
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(error, ct);
    }
}

/// <summary>
/// Base endpoint class for endpoints without request body.
/// </summary>
public abstract class BaseEndpointWithoutRequest<TResponse> : EndpointWithoutRequest<TResponse>
{
    /// <summary>
    /// Gets the authenticated Telegram user ID.
    /// </summary>
    protected long GetUserId()
    {
        var userId = User.GetTelegramId();
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("User is not authenticated");
        }
        return userId.Value;
    }

    /// <summary>
    /// Gets the Telegram user info from the current request.
    /// </summary>
    protected TelegramUser? GetTelegramUser()
    {
        return HttpContext.GetTelegramUser();
    }

    /// <summary>
    /// Sends a 404 Not Found response.
    /// </summary>
    protected Task SendNotFoundAsync(string message = "Resource not found", CancellationToken ct = default)
    {
        HttpContext.Response.StatusCode = 404;
        return HttpContext.Response.WriteAsJsonAsync(ApiErrorResponse.NotFound(message), ct);
    }

    /// <summary>
    /// Sends a 400 Bad Request response.
    /// </summary>
    protected Task SendBadRequestAsync(string message = "Bad request", CancellationToken ct = default)
    {
        HttpContext.Response.StatusCode = 400;
        return HttpContext.Response.WriteAsJsonAsync(ApiErrorResponse.BadRequest(message), ct);
    }
}

/// <summary>
/// Base endpoint class for endpoints without response body.
/// </summary>
public abstract class BaseEndpointWithoutResponse<TRequest> : Endpoint<TRequest>
    where TRequest : notnull
{
    /// <summary>
    /// Gets the authenticated Telegram user ID.
    /// </summary>
    protected long GetUserId()
    {
        var userId = User.GetTelegramId();
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("User is not authenticated");
        }
        return userId.Value;
    }

    /// <summary>
    /// Gets the Telegram user info from the current request.
    /// </summary>
    protected TelegramUser? GetTelegramUser()
    {
        return HttpContext.GetTelegramUser();
    }

    /// <summary>
    /// Sends a 404 Not Found response.
    /// </summary>
    protected Task SendNotFoundAsync(string message = "Resource not found", CancellationToken ct = default)
    {
        HttpContext.Response.StatusCode = 404;
        return HttpContext.Response.WriteAsJsonAsync(ApiErrorResponse.NotFound(message), ct);
    }

    /// <summary>
    /// Sends a 409 Conflict response with location delete details.
    /// </summary>
    protected Task SendLocationDeleteConflictAsync(int childCount, int itemCount, int totalDescendantItems, CancellationToken ct = default)
    {
        HttpContext.Response.StatusCode = 409;
        return HttpContext.Response.WriteAsJsonAsync(new LocationDeleteConflictResponse
        {
            ChildCount = childCount,
            ItemCount = itemCount,
            TotalDescendantItems = totalDescendantItems
        }, ct);
    }
}
