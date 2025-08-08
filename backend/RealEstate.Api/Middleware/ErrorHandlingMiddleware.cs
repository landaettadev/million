using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace RealEstate.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        
        var errorResponse = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                TraceId = traceId,
                Error = "Validation failed",
                Details = validationEx.Errors?.Select(e => e.ErrorMessage).ToArray(),
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            ArgumentException argEx => new ErrorResponse
            {
                TraceId = traceId,
                Error = "Invalid argument",
                Details = new[] { argEx.Message },
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            KeyNotFoundException notFoundEx => new ErrorResponse
            {
                TraceId = traceId,
                Error = "Resource not found",
                Details = new[] { notFoundEx.Message },
                StatusCode = (int)HttpStatusCode.NotFound
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                TraceId = traceId,
                Error = "Unauthorized access",
                StatusCode = (int)HttpStatusCode.Unauthorized
            },
            TimeoutException => new ErrorResponse
            {
                TraceId = traceId,
                Error = "Request timeout",
                Details = new[] { "The operation took too long to complete" },
                StatusCode = (int)HttpStatusCode.RequestTimeout
            },
            _ => new ErrorResponse
            {
                TraceId = traceId,
                Error = "Internal server error",
                Details = new[] { "An unexpected error occurred" },
                StatusCode = (int)HttpStatusCode.InternalServerError
            }
        };

        context.Response.StatusCode = errorResponse.StatusCode;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string TraceId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string[]? Details { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Custom exceptions
public class ValidationException : Exception
{
    public IEnumerable<ValidationError>? Errors { get; }

    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, IEnumerable<ValidationError> errors) : base(message)
    {
        Errors = errors;
    }
}

public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
