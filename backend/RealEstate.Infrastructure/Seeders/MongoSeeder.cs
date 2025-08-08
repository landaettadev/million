using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace RealEstate.Infrastructure;

public sealed class MongoSeeder
{
    private readonly MongoContext _ctx;
    private readonly IConfiguration _config;

    public MongoSeeder(MongoContext ctx, IConfiguration config)
    {
        _ctx = ctx;
        _config = config;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        // Indexes
        await EnsureIndexesAsync(ct);

        var seedEnabled = _config.GetValue<bool>("Seed:Enabled");
        var insertIfEmpty = _config.GetValue<bool>("Seed:InsertIfEmpty");
        if (!seedEnabled) return;

        var count = await _ctx.Properties.CountDocumentsAsync(FilterDefinition<PropertyDocument>.Empty, cancellationToken: ct);
        if (count > 0 && insertIfEmpty) return;

        var random = new Random(42);
        var docs = Enumerable.Range(1, _config.GetValue<int>("Seed:Count", 12)).Select(i => new PropertyDocument
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = $"Property {i}",
            Address = i % 2 == 0 ? "Beverly Hills, CA" : "Manhattan, NY",
            Price = random.Next(100_000, 5_000_000),
            OperationType = i % 2 == 0 ? "sale" : "rent",
            Beds = random.Next(2, 6),
            Baths = random.Next(1, 4),
            HalfBaths = random.Next(0, 2),
            Sqft = random.Next(900, 6000)
        }).ToList();

        await _ctx.Properties.InsertManyAsync(docs, cancellationToken: ct);

        // images
        var images = new List<PropertyImageDocument>();
        foreach (var d in docs)
        {
            var enabledFirst = true;
            var countImages = random.Next(3, 6);
            for (var j = 0; j < countImages; j++)
            {
                images.Add(new PropertyImageDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    PropertyId = d.Id,
                    File = $"https://picsum.photos/id/{200 + random.Next(1, 500)}/1600/1000",
                    Enabled = enabledFirst
                });
                enabledFirst = false;
            }
        }
        await _ctx.PropertyImages.InsertManyAsync(images, cancellationToken: ct);
    }

    private async Task EnsureIndexesAsync(CancellationToken ct)
    {
        // Properties indexes
        var props = _ctx.Properties;

        // 1) price asc (idempotente)
        await props.Indexes.CreateOneAsync(
            new CreateIndexModel<PropertyDocument>(Builders<PropertyDocument>.IndexKeys.Ascending(x => x.Price)),
            cancellationToken: ct);

        // 2) Single text index allowed in Mongo. If none exists, create compound text (name + address).
        var existing = await props.Indexes.ListAsync(cancellationToken: ct);
        var indexDocs = await existing.ToListAsync(ct);
        var hasText = indexDocs.Any(d => d.GetElement("key").Value.AsBsonDocument.Names.Contains("_fts"));
        if (!hasText)
        {
            var textKeys = new BsonDocument { { "name", "text" }, { "address", "text" } };
            var textModel = new CreateIndexModel<PropertyDocument>(textKeys, new CreateIndexOptions { Name = "search_text" });
            await props.Indexes.CreateOneAsync(textModel, cancellationToken: ct);
        }

        // Images index
        var imgIdx = new CreateIndexModel<PropertyImageDocument>(Builders<PropertyImageDocument>.IndexKeys
            .Ascending(x => x.PropertyId)
            .Ascending(x => x.Enabled));
        await _ctx.PropertyImages.Indexes.CreateOneAsync(imgIdx, cancellationToken: ct);
    }
}
