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
            ThumbnailFile = dto.ThumbnailFile,
            Enabled = dto.Enabled,
            Order = dto.Order,
            FileSize = dto.FileSize,
            ContentType = dto.ContentType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        await _ctx.PropertyImages.InsertOneAsync(doc, cancellationToken: ct);
        return doc.Id;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var update = Builders<PropertyImageDocument>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var res = await _ctx.PropertyImages.UpdateOneAsync(x => x.Id == id && !x.IsDeleted, update, cancellationToken: ct);
        return res.ModifiedCount > 0;
    }
}


