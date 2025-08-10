using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

public static class AdminOwnerEndpoints
{
    public static IEndpointRouteBuilder MapAdminOwnerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin/owners")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapPost("/", async (
            CreateOwnerDto dto,
            IValidator<CreateOwnerDto> validator,
            IOwnerWriteService svc,
            HttpContext ctx) =>
        {
            var v = await validator.ValidateAsync(dto, ctx.RequestAborted);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var id = await svc.CreateAsync(dto, ctx.RequestAborted);
            return Results.Created($"/api/admin/owners/{id}", new { id });
        });

        group.MapPut("/{id}", async (
            string id,
            UpdateOwnerDto dto,
            IValidator<UpdateOwnerDto> validator,
            IOwnerWriteService svc,
            IValidator<string> idValidator,
            HttpContext ctx) =>
        {
            var idRes = await idValidator.ValidateAsync(id, ctx.RequestAborted);
            if (!idRes.IsValid) return Results.ValidationProblem(idRes.ToDictionary());

            var v = await validator.ValidateAsync(dto, ctx.RequestAborted);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var ok = await svc.UpdateAsync(id, dto, ctx.RequestAborted);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id}", async (
            string id,
            IOwnerWriteService svc,
            IValidator<string> idValidator,
            HttpContext ctx) =>
        {
            var idRes = await idValidator.ValidateAsync(id, ctx.RequestAborted);
            if (!idRes.IsValid) return Results.ValidationProblem(idRes.ToDictionary());
            var ok = await svc.DeleteAsync(id, ctx.RequestAborted);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return endpoints;
    }
}


