using MongoDB.Driver;
using RealEstate.Application;

namespace RealEstate.Infrastructure;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly MongoContext _ctx;

    public AdminUserRepository(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var doc = await _ctx.AdminUsers.Find(u => u.Email == email).FirstOrDefaultAsync(ct);
        if (doc is null) return null;
        return new AdminUser(doc.Id, doc.Email, doc.Name, doc.Role, doc.PasswordHash);
    }

    public async Task CreateAsync(AdminUser user, CancellationToken ct = default)
    {
        var doc = new AdminUserDocument
        {
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            PasswordHash = user.PasswordHash
        };
        await _ctx.AdminUsers.InsertOneAsync(doc, cancellationToken: ct);
    }
}


