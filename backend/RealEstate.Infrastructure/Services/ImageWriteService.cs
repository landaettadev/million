using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.Application;

namespace RealEstate.Infrastructure;

public sealed class ImageWriteService : IImageWriteService
{
    private readonly MongoContext _ctx;

    public ImageWriteService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<string> AddAsync(AddImageDto dto, CancellationToken ct = default)
    {
        var doc = new PropertyImageDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PropertyId = dto.PropertyId,
            File = dto.File,
            Enabled = dto.Enabled,
            Order = dto.Order
        };
        await _ctx.PropertyImages.InsertOneAsync(doc, cancellationToken: ct);
        return doc.Id;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var res = await _ctx.PropertyImages.DeleteOneAsync(x => x.Id == id, ct);
        return res.DeletedCount > 0;
    }
}


