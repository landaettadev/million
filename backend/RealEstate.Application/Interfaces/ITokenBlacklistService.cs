using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public interface ITokenBlacklistService
{
    Task AddToBlacklistAsync(string token, string userId, string reason, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
    Task<bool> IsBlacklistedAsync(string token, CancellationToken ct = default);
    Task CleanupExpiredBlacklistedTokensAsync(CancellationToken ct = default);
}
