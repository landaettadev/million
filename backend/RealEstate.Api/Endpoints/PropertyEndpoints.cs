using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

public static class PropertyEndpoints
{
    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/properties");

        group.MapGet("/", async (HttpContext ctx, IPropertyReadService service, IValidator<SearchPropertiesQuery> validator) =>
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

            // Validate query parameters
            var validationResult = await validator.ValidateAsync(q, ctx.RequestAborted);
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                throw new ValidationException(string.Join("; ", errorMessages));
            }

            var result = await service.SearchAsync(q, ctx.RequestAborted);
            var payload = new PropertyListResponse(result.Items, result.Page, result.PageSize, result.Total);
            return Results.Json(payload);
        });

        group.MapGet("/featured", async (HttpContext ctx, IPropertyReadService service) =>
        {
            var limit = int.TryParse(ctx.Request.Query["limit"], out var l) ? l : 6;
            var properties = await service.GetFeaturedPropertiesAsync(limit, ctx.RequestAborted);
            return Results.Json(properties);
        });

        group.MapGet("/{id}", async (string id, IPropertyReadService service, IValidator<string> idValidator, HttpContext ctx) =>
        {
            // Validate ObjectId format using FluentValidation
            var idValidationResult = await idValidator.ValidateAsync(id, ctx.RequestAborted);
            if (!idValidationResult.IsValid)
            {
                var errorMessages = idValidationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                throw new ValidationException(string.Join("; ", errorMessages));
            }

            var item = await service.GetByIdAsync(id, ctx.RequestAborted);
            if (item is null)
            {
                throw new KeyNotFoundException($"Property with ID '{id}' not found");
            }
            return Results.Json(item);
        });

        // Public video url for a property
        group.MapGet("/{id}/video", async (string id, IPropertyReadService service, HttpContext ctx) =>
        {
            var url = await service.GetVideoUrlAsync(id, ctx.RequestAborted);
            return Results.Json(new { id, url });
        });

        return endpoints;
    }
}

file record PropertyListResponse(IReadOnlyList<PropertyLiteDto> items, int page, int pageSize, long total);
