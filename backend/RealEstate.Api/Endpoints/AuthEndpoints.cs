using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/login", async (LoginRequest req, IAuthService auth, HttpContext ctx) =>
        {
            var result = await auth.LoginAsync(req.Email, req.Password, ctx.RequestAborted);
            if (result is null) return Results.Unauthorized();
            var (token, user) = result.Value;
            return Results.Ok(new { token, user = new { user.Email, user.Name, user.Role } });
        });

        return endpoints;
    }

    public sealed record LoginRequest(string Email, string Password);
}


