using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public sealed record CreateOwnerDto(
    string Name,
    string Address,
    string? Photo,
    System.DateTime? Birthday
);

public sealed record UpdateOwnerDto(
    string Name,
    string Address,
    string? Photo,
    System.DateTime? Birthday
);

public interface IOwnerWriteService
{
    Task<string> CreateAsync(CreateOwnerDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(string id, UpdateOwnerDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}


