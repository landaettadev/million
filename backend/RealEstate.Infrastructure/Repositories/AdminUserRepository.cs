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

    public async Task<AdminUser?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _ctx.AdminUsers.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
        if (doc is null) return null;
        return new AdminUser(doc.Id, doc.Email, doc.Name, doc.Role, doc.PasswordHash);
    }

    public async Task<List<AdminUser>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await _ctx.AdminUsers.Find(_ => true).ToListAsync(ct);
        return docs.Select(doc => new AdminUser(doc.Id, doc.Email, doc.Name, doc.Role, doc.PasswordHash)).ToList();
    }

    public async Task<AdminUser> CreateAsync(AdminUser user, CancellationToken ct = default)
    {
        var doc = new AdminUserDocument
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            PasswordHash = user.PasswordHash
        };
        await _ctx.AdminUsers.InsertOneAsync(doc, cancellationToken: ct);
        return user;
    }

    public async Task<AdminUser> UpdateAsync(AdminUser user, CancellationToken ct = default)
    {
        var update = Builders<AdminUserDocument>.Update
            .Set(u => u.Email, user.Email)
            .Set(u => u.Name, user.Name)
            .Set(u => u.Role, user.Role)
            .Set(u => u.PasswordHash, user.PasswordHash);

        await _ctx.AdminUsers.UpdateOneAsync(u => u.Id == user.Id, update, cancellationToken: ct);
        return user;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _ctx.AdminUsers.DeleteOneAsync(u => u.Id == id, cancellationToken: ct);
    }
}


