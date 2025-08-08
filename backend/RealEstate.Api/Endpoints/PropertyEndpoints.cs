using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

public static class PropertyEndpoints
{
    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/properties");

        group.MapGet("/", async (HttpContext ctx, IPropertyReadService service) =>
        {
            var q = new SearchPropertiesQuery(
                Name: ctx.Request.Query["name"].ToString(),
                Address: ctx.Request.Query["address"].ToString(),
                MinPrice: decimal.TryParse(ctx.Request.Query["minPrice"], out var min) ? min : null,
                MaxPrice: decimal.TryParse(ctx.Request.Query["maxPrice"], out var max) ? max : null,
                OperationType: Enum.TryParse<OperationType>(ctx.Request.Query["operationType"], true, out var op) ? op : null,
                Page: int.TryParse(ctx.Request.Query["page"], out var page) ? page : PagingDefaults.DefaultPage,
                PageSize: int.TryParse(ctx.Request.Query["pageSize"], out var size) ? size : PagingDefaults.DefaultPageSize
            );

            var result = await service.SearchAsync(q, ctx.RequestAborted);
            return Results.Ok(new { items = result.Items, page = result.Page, pageSize = result.PageSize, total = result.Total });
        });

        group.MapGet("/{id}", async (string id, IPropertyReadService service, HttpContext ctx) =>
        {
            var item = await service.GetByIdAsync(id, ctx.RequestAborted);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        return endpoints;
    }
}
