using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RealEstate.Api.Middleware;
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
                var errors = validationResult.Errors.Select(e => new ValidationError 
                { 
                    PropertyName = e.PropertyName, 
                    ErrorMessage = e.ErrorMessage 
                }).ToArray();
                
                throw new RealEstate.Api.Middleware.ValidationException("Invalid query parameters", errors);
            }

            var result = await service.SearchAsync(q, ctx.RequestAborted);
            return Results.Ok(new { items = result.Items, page = result.Page, pageSize = result.PageSize, total = result.Total });
        });

        group.MapGet("/{id}", async (string id, IPropertyReadService service, IValidator<string> idValidator, HttpContext ctx) =>
        {
            // Validate ObjectId format using FluentValidation
            var idValidationResult = await idValidator.ValidateAsync(id, ctx.RequestAborted);
            if (!idValidationResult.IsValid)
            {
                var errors = idValidationResult.Errors.Select(e => new ValidationError 
                { 
                    PropertyName = e.PropertyName, 
                    ErrorMessage = e.ErrorMessage 
                }).ToArray();
                
                throw new RealEstate.Api.Middleware.ValidationException("Invalid property ID", errors);
            }

            var item = await service.GetByIdAsync(id, ctx.RequestAborted);
            if (item is null)
            {
                throw new KeyNotFoundException($"Property with ID '{id}' not found");
            }
            
            return Results.Ok(item);
        });

        return endpoints;
    }
}
