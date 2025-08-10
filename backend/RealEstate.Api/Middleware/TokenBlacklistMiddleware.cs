using Microsoft.AspNetCore.Http;
using RealEstate.Application;

namespace RealEstate.Api.Middleware;

public sealed class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
    {
        var token = ExtractTokenFromHeader(context);
        
        if (!string.IsNullOrEmpty(token))
        {
            // Check if token is blacklisted
            if (await blacklistService.IsBlacklistedAsync(token, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token has been invalidated" });
                return;
            }
        }

        await _next(context);
    }

    private static string ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return string.Empty;

        return authHeader.Substring("Bearer ".Length);
    }
}
