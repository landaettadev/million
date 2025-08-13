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

public sealed record UpdatePropertyVideoDto(
    string Url
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

public sealed record OwnerDto(
    string Id,
    string Name,
    string Address,
    string? Photo,
    DateTime? Birthday,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record AdminOwnerDto(
    string Id,
    string Name,
    string Address,
    string? Photo,
    DateTime? Birthday,
    int PropertiesCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);

public sealed record CreateOwnerDto(
    string Name,
    string Address,
    string? Photo = null,
    DateTime? Birthday = null
);

public sealed record UpdateOwnerDto(
    string Name,
    string Address,
    string? Photo = null,
    DateTime? Birthday = null
);

public sealed record AdminPropertyDto(
    string Id,
    string OwnerId,
    string OwnerName,
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    DateTime CreatedAt,
    bool IsDeleted,
    string? Description = null,
    int? Beds = null,
    int? Baths = null,
    int? HalfBaths = null,
    int? Sqft = null,
    DateTime? UpdatedAt = null
);

public sealed record CreatePropertyDto(
    string OwnerId,
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    int? Beds = null,
    int? Baths = null,
    int? HalfBaths = null,
    int? Sqft = null,
    string? Description = null
);

public sealed record UpdatePropertyDto(
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    int? Beds = null,
    int? Baths = null,
    int? HalfBaths = null,
    int? Sqft = null,
    string? Description = null
);

public sealed record UpdatePropertyImageDto(
    string FileName,
    bool IsMain,
    bool IsEnabled,
    int Order
);

public sealed record AnalyticsDto(
    int TotalProperties,
    int TotalOwners,
    decimal TotalRevenue,
    decimal MonthlyRevenue,
    decimal YearlyRevenue
);

public sealed record AdminUser(
    string Id,
    string Email,
    string Name,
    string Role,
    string PasswordHash
);

public interface IPropertyReadService
{
    Task<PagedResult<PropertyLiteDto>> SearchAsync(SearchPropertiesQuery query, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<PropertyLiteDto>> GetFeaturedPropertiesAsync(int limit = 6, CancellationToken cancellationToken = default);
    Task<string?> GetVideoUrlAsync(string id, CancellationToken cancellationToken = default);
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
    Task<PagedResult<AdminOwnerDto>> GetOwnersAsync(int page = 1, int pageSize = 10, CancellationToken ct = default);
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
    Task SetPropertyVideoAsync(string id, string url, CancellationToken ct = default);
    Task<string?> GetPropertyVideoAsync(string id, CancellationToken ct = default);
    Task<bool> SetFeaturedAsync(string id, bool isFeatured, CancellationToken ct = default);
}

public interface IAdminAnalyticsService
{
    Task<AnalyticsDto> GetAnalyticsAsync(CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemovePatternAsync(string pattern, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default);
}
