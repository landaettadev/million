using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RealEstate.Application.DTOs;

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

public sealed record PropertyImageDto(
    string Id,
    string FileName,
    string Url,
    long Size,
    string ContentType,
    bool IsMain,
    bool IsEnabled,
    int Order,
    DateTime UploadedAt
);

public sealed record OwnerDetailDto(
    string Id,
    string Name,
    string Email,
    string Phone,
    string Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public interface IPropertyReadService
{
    Task<PagedResult<PropertyLiteDto>> SearchAsync(SearchPropertiesQuery query, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<PropertyLiteDto>> GetFeaturedPropertiesAsync(int limit = 6, CancellationToken cancellationToken = default);
}

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AdminUser?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<AdminUser>> GetAllAsync(CancellationToken ct = default);
    Task<AdminUser> CreateAsync(AdminUser user, CancellationToken ct = default);
    Task<AdminUser> UpdateAsync(AdminUser user, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public interface IImageStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string fileName, CancellationToken ct = default);
    string GetImageUrl(string imagePath);
}

public interface IAdminImageReadService
{
    Task<List<PropertyImageDto>> GetPropertyImagesAsync(string propertyId, CancellationToken ct = default);
}

public interface IAdminImageUploadService
{
    Task<PropertyImageDto> UploadImageAsync(string propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteImageAsync(string imageId, CancellationToken ct = default);
    Task<PropertyImageDto> UpdateImageAsync(string imageId, UpdatePropertyImageDto updateDto, CancellationToken ct = default);
}

public interface IAdminOwnerService
{
    Task<PagedResult<OwnerDto>> GetOwnersAsync(int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<OwnerDto?> GetOwnerByIdAsync(string id, CancellationToken ct = default);
    Task<OwnerDto> CreateOwnerAsync(CreateOwnerDto createDto, CancellationToken ct = default);
    Task<OwnerDto> UpdateOwnerAsync(string id, UpdateOwnerDto updateDto, CancellationToken ct = default);
    Task DeleteOwnerAsync(string id, CancellationToken ct = default);
}

public interface IAdminPropertyService
{
    Task<PagedResult<AdminPropertyDto>> GetPropertiesAsync(int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<AdminPropertyDto?> GetPropertyByIdAsync(string id, CancellationToken ct = default);
    Task<AdminPropertyDto> CreatePropertyAsync(CreatePropertyDto createDto, CancellationToken ct = default);
    Task<AdminPropertyDto> UpdatePropertyAsync(string id, UpdatePropertyDto updateDto, CancellationToken ct = default);
    Task DeletePropertyAsync(string id, CancellationToken ct = default);
}

public interface IAdminAnalyticsService
{
    Task<AnalyticsDto> GetAnalyticsAsync(CancellationToken ct = default);
}
