using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.Application;

namespace RealEstate.Infrastructure;

public sealed class PropertyWriteService : IPropertyWriteService
{
    private readonly MongoContext _ctx;

    public PropertyWriteService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<string> CreateAsync(CreatePropertyDto dto, CancellationToken ct = default)
    {
        var doc = new PropertyDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            OwnerId = dto.OwnerId,
            Name = dto.Name,
            Address = dto.Address,
            Price = dto.Price,
            OperationType = dto.OperationType == OperationType.Sale ? "sale" : "rent",
            Beds = dto.Beds,
            Baths = dto.Baths,
            HalfBaths = dto.HalfBaths,
            Sqft = dto.Sqft,
            Description = dto.Description
        };
        await _ctx.Properties.InsertOneAsync(doc, cancellationToken: ct);
        return doc.Id;
    }

    public async Task<bool> UpdateAsync(string id, UpdatePropertyDto dto, CancellationToken ct = default)
    {
        var update = Builders<PropertyDocument>.Update
            .Set(x => x.Name, dto.Name)
            .Set(x => x.Address, dto.Address)
            .Set(x => x.Price, dto.Price)
            .Set(x => x.OperationType, dto.OperationType == OperationType.Sale ? "sale" : "rent")
            .Set(x => x.Beds, dto.Beds)
            .Set(x => x.Baths, dto.Baths)
            .Set(x => x.HalfBaths, dto.HalfBaths)
            .Set(x => x.Sqft, dto.Sqft)
            .Set(x => x.Description, dto.Description);

        var res = await _ctx.Properties.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return res.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var res = await _ctx.Properties.DeleteOneAsync(x => x.Id == id, ct);
        return res.DeletedCount > 0;
    }
}


