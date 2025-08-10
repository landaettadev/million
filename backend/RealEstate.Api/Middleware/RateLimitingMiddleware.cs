using System.Collections.Concurrent;
using System.Net;

namespace RealEstate.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, LoginAttempt> _loginAttempts = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lockedOutIps = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lockedOutUsers = new();

    private const int MaxLoginAttempts = 5;
    private const int LockoutDurationMinutes = 15;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsLoginEndpoint(context.Request))
        {
            var ipAddress = GetClientIpAddress(context);
            var userIdentifier = GetUserIdentifier(context);

            // Check if IP is locked out
            if (_lockedOutIps.TryGetValue(ipAddress, out var ipLockoutTime))
            {
                if (DateTime.UtcNow < ipLockoutTime)
                {
                    _logger.LogWarning("Login blocked for locked out IP: {IP}", ipAddress);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await context.Response.WriteAsJsonAsync(new { error = "Too many login attempts. Please try again later." });
                    return;
                }
                else
                {
                    _lockedOutIps.TryRemove(ipAddress, out _);
                }
            }

            // Check if user is locked out
            if (!string.IsNullOrEmpty(userIdentifier) && _lockedOutUsers.TryGetValue(userIdentifier, out var userLockoutTime))
            {
                if (DateTime.UtcNow < userLockoutTime)
                {
                    _logger.LogWarning("Login blocked for locked out user: {User}", userIdentifier);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await context.Response.WriteAsJsonAsync(new { error = "Too many login attempts. Please try again later." });
                    return;
                }
                else
                {
                    _lockedOutUsers.TryRemove(userIdentifier, out _);
                }
            }

            // Track the request
            TrackLoginAttempt(ipAddress, userIdentifier);
        }

        await _next(context);
    }

    private static bool IsLoginEndpoint(HttpRequest request)
    {
        return request.Method == "POST" && 
               request.Path.StartsWithSegments("/api/admin/auth/login", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Try to get the real IP from headers (for proxy scenarios)
        var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            return forwardedHeader.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string? GetUserIdentifier(HttpContext context)
    {
        // Try to read the request body to get email for user-based tracking
        // Note: This is a simplified approach. In production, you might want to use a different strategy
        return null; // For now, we'll only track by IP to avoid complexity
    }

    private void TrackLoginAttempt(string ipAddress, string? userIdentifier)
    {
        var now = DateTime.UtcNow;
        
        // Track by IP
        var ipKey = $"ip:{ipAddress}";
        var ipAttempts = _loginAttempts.GetOrAdd(ipKey, _ => new LoginAttempt());
        
        if (ipAttempts.Attempts.Count >= MaxLoginAttempts)
        {
            var oldestAttempt = ipAttempts.Attempts.First();
            if (now - oldestAttempt < TimeSpan.FromMinutes(LockoutDurationMinutes))
            {
                // Still in lockout period
                return;
            }
            // Reset attempts after lockout period
            ipAttempts.Attempts.Clear();
        }

        ipAttempts.Attempts.Add(now);
        
        // Remove old attempts outside the window
        ipAttempts.Attempts.RemoveAll(t => now - t > TimeSpan.FromMinutes(5));

        // Check if we should lockout
        if (ipAttempts.Attempts.Count >= MaxLoginAttempts)
        {
            var lockoutUntil = now.AddMinutes(LockoutDurationMinutes);
            _lockedOutIps.AddOrUpdate(ipAddress, lockoutUntil, (_, _) => lockoutUntil);
            _logger.LogWarning("IP {IP} locked out until {LockoutUntil}", ipAddress, lockoutUntil);
        }

        // Track by user if available
        if (!string.IsNullOrEmpty(userIdentifier))
        {
            var userKey = $"user:{userIdentifier}";
            var userAttempts = _loginAttempts.GetOrAdd(userKey, _ => new LoginAttempt());
            
            if (userAttempts.Attempts.Count >= MaxLoginAttempts)
            {
                var oldestAttempt = userAttempts.Attempts.First();
                if (now - oldestAttempt < TimeSpan.FromMinutes(LockoutDurationMinutes))
                {
                    return;
                }
                userAttempts.Attempts.Clear();
            }

            userAttempts.Attempts.Add(now);
            userAttempts.Attempts.RemoveAll(t => now - t > TimeSpan.FromMinutes(5));

            if (userAttempts.Attempts.Count >= MaxLoginAttempts)
            {
                var lockoutUntil = now.AddMinutes(LockoutDurationMinutes);
                _lockedOutUsers.AddOrUpdate(userIdentifier, lockoutUntil, (_, _) => lockoutUntil);
                _logger.LogWarning("User {User} locked out until {LockoutUntil}", userIdentifier, lockoutUntil);
            }
        }
    }

    private class LoginAttempt
    {
        public List<DateTime> Attempts { get; } = new();
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
