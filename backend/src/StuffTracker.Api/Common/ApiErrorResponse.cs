using System.Diagnostics;

namespace StuffTracker.Api.Common;

/// <summary>
/// Standard error response format per API contract.
/// </summary>
public class ApiErrorResponse
{
    public string Type { get; set; } = "error";
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public string? TraceId { get; set; }

    public static ApiErrorResponse Create(int statusCode, string message, Dictionary<string, string[]>? errors = null)
    {
        return new ApiErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
            TraceId = Activity.Current?.Id ?? Guid.NewGuid().ToString()
        };
    }

    public static ApiErrorResponse NotFound(string message = "Resource not found")
        => Create(404, message);

    public static ApiErrorResponse BadRequest(string message = "Bad request")
        => Create(400, message);

    public static ApiErrorResponse ValidationError(string message = "Validation failed", Dictionary<string, string[]>? errors = null)
        => Create(400, message, errors);

    public static ApiErrorResponse Unauthorized(string message = "Invalid or missing Telegram authentication")
        => Create(401, message);

    public static ApiErrorResponse Conflict(string message = "Conflict")
        => Create(409, message);

    public static ApiErrorResponse InternalError(string message = "An unexpected error occurred")
        => Create(500, message);
}

/// <summary>
/// Conflict response for location delete with contents.
/// </summary>
public class LocationDeleteConflictResponse : ApiErrorResponse
{
    public int ChildCount { get; set; }
    public int ItemCount { get; set; }
    public int TotalDescendantItems { get; set; }

    public LocationDeleteConflictResponse()
    {
        Type = "conflict";
        StatusCode = 409;
        Message = "Location has contents. Use force=true to delete.";
    }
}
