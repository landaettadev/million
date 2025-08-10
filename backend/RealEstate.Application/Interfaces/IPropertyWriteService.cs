using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public sealed record CreatePropertyDto(
    string OwnerId,
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    int? Beds,
    int? Baths,
    int? HalfBaths,
    int? Sqft,
    string? Description
);

public sealed record UpdatePropertyDto(
    string Name,
    string Address,
    decimal Price,
    OperationType OperationType,
    int? Beds,
    int? Baths,
    int? HalfBaths,
    int? Sqft,
    string? Description
);

public interface IPropertyWriteService
{
    Task<string> CreateAsync(CreatePropertyDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(string id, UpdatePropertyDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}


