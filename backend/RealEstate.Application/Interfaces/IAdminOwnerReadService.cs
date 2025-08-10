using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

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

public sealed record AdminOwnerSearchQuery(
    string? SearchTerm = null,
    string? SortBy = "CreatedAt",
    string SortDirection = "desc",
    int Page = 1,
    int PageSize = 20
);

public interface IAdminOwnerReadService
{
    Task<PagedResult<AdminOwnerDto>> SearchAsync(AdminOwnerSearchQuery query, CancellationToken ct = default);
    Task<AdminOwnerDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<long> GetTotalCountAsync(CancellationToken ct = default);
}

public sealed record AdminOwnerDetailDto(
    string Id,
    string Name,
    string Address,
    string? Photo,
    DateTime? Birthday,
    IReadOnlyList<AdminOwnerPropertyDto> Properties,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsDeleted
);

public sealed record AdminOwnerPropertyDto(
    string Id,
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    DateTime CreatedAt
);
