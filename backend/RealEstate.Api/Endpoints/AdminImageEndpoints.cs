using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

public static class AdminImageEndpoints
{
    public static IEndpointRouteBuilder MapAdminImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin/images")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapPost("/", async (AddImageDto dto, IImageWriteService svc, HttpContext ctx) =>
        {
            var id = await svc.AddAsync(dto, ctx.RequestAborted);
            return Results.Created($"/api/admin/images/{id}", new { id });
        });

        group.MapDelete("/{id}", async (string id, IImageWriteService svc, HttpContext ctx) =>
        {
            var ok = await svc.DeleteAsync(id, ctx.RequestAborted);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return endpoints;
    }
}


