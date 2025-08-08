using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public static class PagingDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
}

public enum OperationType
{
    Sale,
    Rent
}

public sealed record PropertyLiteDto(
    string Id,
    string IdOwner,
    string Name,
    string Address,
    decimal Price,
    string? Image,
    OperationType? OperationType,
    int? Beds = null,
    int? Baths = null,
    int? HalfBaths = null,
    int? Sqft = null
);

public sealed record PropertyDetailDto(
    string Id,
    string IdOwner,
    string Name,
    string Address,
    decimal Price,
    IReadOnlyList<string> Images,
    OperationType? OperationType,
    string? Description = null,
    int? Beds = null,
    int? Baths = null,
    int? HalfBaths = null,
    int? Sqft = null
);

public sealed record SearchPropertiesQuery(
    string? Name = null,
    string? Address = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    OperationType? OperationType = null,
    int Page = PagingDefaults.DefaultPage,
    int PageSize = PagingDefaults.DefaultPageSize
);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total
);

public interface IPropertyReadService
{
    Task<PagedResult<PropertyLiteDto>> SearchAsync(SearchPropertiesQuery query, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
