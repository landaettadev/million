using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

public static class AdminPropertyEndpoints
{
    public static IEndpointRouteBuilder MapAdminPropertyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin/properties")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapPost("/", async (
            CreatePropertyDto dto,
            IValidator<CreatePropertyDto> validator,
            IPropertyWriteService svc,
            HttpContext ctx) =>
        {
            var v = await validator.ValidateAsync(dto, ctx.RequestAborted);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var id = await svc.CreateAsync(dto, ctx.RequestAborted);
            return Results.Created($"/api/admin/properties/{id}", new { id });
        });

        group.MapPut("/{id}", async (
            string id,
            UpdatePropertyDto dto,
            IValidator<UpdatePropertyDto> validator,
            IPropertyWriteService svc,
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
            IPropertyWriteService svc,
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


