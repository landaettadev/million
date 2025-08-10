using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public sealed record AdminPropertyDto(
    string Id,
    string OwnerId,
    string OwnerName,
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    string? Description,
    int? Beds,
    int? Baths,
    int? HalfBaths,
    int? Sqft,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);

public sealed record AdminPropertySearchQuery(
    string? SearchTerm = null,
    string? OwnerId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    OperationType? OperationType = null,
    string? SortBy = "CreatedAt",
    string SortDirection = "desc",
    int Page = 1,
    int PageSize = 20
);

public interface IAdminPropertyReadService
{
    Task<PagedResult<AdminPropertyDto>> SearchAsync(AdminPropertySearchQuery query, CancellationToken ct = default);
    Task<AdminPropertyDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<long> GetTotalCountAsync(CancellationToken ct = default);
}

public sealed record AdminPropertyDetailDto(
    string Id,
    string OwnerId,
    string OwnerName,
    string OwnerAddress,
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    string? Description,
    int? Beds,
    int? Baths,
    int? HalfBaths,
    int? Sqft,
    IReadOnlyList<AdminPropertyImageDto> Images,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);

public sealed record AdminPropertyImageDto(
    string Id,
    string File,
    bool Enabled,
    int Order,
    DateTime CreatedAt
);
