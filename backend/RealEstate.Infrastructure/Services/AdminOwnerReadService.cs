using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminOwnerService : IAdminOwnerService
{
    private readonly MongoContext _ctx;

    public AdminOwnerService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResult<AdminOwnerDto>> GetOwnersAsync(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var filter = Builders<OwnerDocument>.Filter.Eq(x => x.IsDeleted, false);
        var skip = (page - 1) * pageSize;

        var ownerDocs = await _ctx.Owners
            .Find(filter)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(ct);

        var totalCount = await _ctx.Owners.CountDocumentsAsync(filter, cancellationToken: ct);

        // Build a lookup for properties count per owner
        var ownerIds = ownerDocs.Select(o => o.Id).ToList();
        var counts = await _ctx.Properties
            .Aggregate()
            .Match(p => ownerIds.Contains(p.OwnerId) && !p.IsDeleted)
            .Group(p => p.OwnerId, g => new { OwnerId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countByOwnerId = counts.ToDictionary(x => x.OwnerId, x => x.Count);

        var owners = ownerDocs.Select(o => new AdminOwnerDto(
            Id: o.Id,
            Name: o.Name,
            Address: o.Address,
            Photo: o.Photo,
            Birthday: o.Birthday,
            PropertiesCount: countByOwnerId.TryGetValue(o.Id, out var c) ? c : 0,
            CreatedAt: o.CreatedAt,
            UpdatedAt: o.UpdatedAt,
            IsDeleted: o.IsDeleted
        )).ToList();

        return new PagedResult<AdminOwnerDto>(owners, page, pageSize, totalCount);
    }

    public async Task<OwnerDto?> GetOwnerByIdAsync(string id, CancellationToken ct = default)
    {
        var ownerDoc = await _ctx.Owners.Find(o => o.Id == id && !o.IsDeleted).FirstOrDefaultAsync(ct);
        if (ownerDoc is null) return null;

        return new OwnerDto(
            ownerDoc.Id,
            ownerDoc.Name,
            ownerDoc.Address,
            ownerDoc.Photo,
            ownerDoc.Birthday,
            ownerDoc.CreatedAt,
            ownerDoc.UpdatedAt
        );
    }

    public async Task<OwnerDto> CreateOwnerAsync(CreateOwnerDto createDto, CancellationToken ct = default)
    {
        var ownerDoc = new OwnerDocument
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = createDto.Name,
            Address = createDto.Address,
            Photo = createDto.Photo,
            Birthday = createDto.Birthday,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _ctx.Owners.InsertOneAsync(ownerDoc, cancellationToken: ct);

        return new OwnerDto(
            ownerDoc.Id,
            ownerDoc.Name,
            ownerDoc.Address,
            ownerDoc.Photo,
            ownerDoc.Birthday,
            ownerDoc.CreatedAt,
            ownerDoc.UpdatedAt
        );
    }

    public async Task<OwnerDto> UpdateOwnerAsync(string id, UpdateOwnerDto updateDto, CancellationToken ct = default)
    {
        var update = Builders<OwnerDocument>.Update
            .Set(o => o.Name, updateDto.Name)
            .Set(o => o.Address, updateDto.Address)
            .Set(o => o.Photo, updateDto.Photo)
            .Set(o => o.Birthday, updateDto.Birthday)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        await _ctx.Owners.UpdateOneAsync(o => o.Id == id, update, cancellationToken: ct);

        var updatedOwner = await GetOwnerByIdAsync(id, ct);
        if (updatedOwner == null)
            throw new InvalidOperationException($"Owner {id} not found after update");

        return updatedOwner;
    }

    public async Task DeleteOwnerAsync(string id, CancellationToken ct = default)
    {
        var update = Builders<OwnerDocument>.Update
            .Set(o => o.IsDeleted, true)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        await _ctx.Owners.UpdateOneAsync(o => o.Id == id, update, cancellationToken: ct);
    }
}
