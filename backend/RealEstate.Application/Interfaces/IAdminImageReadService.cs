using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public sealed record AdminImageDto(
    string Id,
    string PropertyId,
    string PropertyName,
    string File,
    bool Enabled,
    int Order,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);

public sealed record AdminImageSearchQuery(
    string? PropertyId = null,
    bool? Enabled = null,
    string? SortBy = "CreatedAt",
    string SortDirection = "desc",
    int Page = 1,
    int PageSize = 20
);

public interface IAdminImageReadService
{
    Task<PagedResult<AdminImageDto>> SearchAsync(AdminImageSearchQuery query, CancellationToken ct = default);
    Task<AdminImageDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<long> GetTotalCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminImageDto>> GetByPropertyIdAsync(string propertyId, CancellationToken ct = default);
}

public sealed record AdminImageDetailDto(
    string Id,
    string PropertyId,
    string PropertyName,
    string PropertyAddress,
    string File,
    bool Enabled,
    int Order,
    long FileSize,
    string ContentType,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);
