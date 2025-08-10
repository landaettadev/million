using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task CreateAsync(AdminUser user, CancellationToken ct = default);
}

public sealed record AdminUser(
    string Id,
    string Email,
    string Name,
    string Role,
    string PasswordHash
);


