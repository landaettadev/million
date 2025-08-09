using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using FluentValidation;

namespace RealEstate.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var errorResponse = new
        {
            traceId = context.TraceIdentifier,
            error = GetErrorTitle(exception),
            details = _environment.IsDevelopment() ? exception.Message : null,
            statusCode = GetStatusCode(exception),
            timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = (int)GetStatusCode(exception);
        
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(jsonResponse);
    }

    private static string GetErrorTitle(Exception exception)
    {
        return exception switch
        {
            ValidationException => "Validation Error",
            KeyNotFoundException => "Resource Not Found",
            ArgumentException => "Invalid Argument",
            _ => "Internal Server Error"
        };
    }

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => HttpStatusCode.BadRequest,
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
