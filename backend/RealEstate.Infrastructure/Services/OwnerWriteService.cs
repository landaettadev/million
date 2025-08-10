using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.Application;

namespace RealEstate.Infrastructure;

public sealed class OwnerWriteService : IOwnerWriteService
{
    private readonly MongoContext _ctx;

    public OwnerWriteService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<string> CreateAsync(CreateOwnerDto dto, CancellationToken ct = default)
    {
        var doc = new OwnerDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = dto.Name,
            Address = dto.Address,
            Photo = dto.Photo,
            Birthday = dto.Birthday
        };
        await _ctx.Owners.InsertOneAsync(doc, cancellationToken: ct);
        return doc.Id;
    }

    public async Task<bool> UpdateAsync(string id, UpdateOwnerDto dto, CancellationToken ct = default)
    {
        var update = Builders<OwnerDocument>.Update
            .Set(x => x.Name, dto.Name)
            .Set(x => x.Address, dto.Address)
            .Set(x => x.Photo, dto.Photo)
            .Set(x => x.Birthday, dto.Birthday);
        var res = await _ctx.Owners.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return res.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var res = await _ctx.Owners.DeleteOneAsync(x => x.Id == id, ct);
        return res.DeletedCount > 0;
    }
}


