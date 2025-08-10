using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public sealed record AddImageDto(
    string PropertyId,
    string File,
    bool Enabled,
    int Order
);

public interface IImageWriteService
{
    Task<string> AddAsync(AddImageDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}


