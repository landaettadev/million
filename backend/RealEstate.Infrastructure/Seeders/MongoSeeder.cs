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

    public MongoSeeder(IMongoDatabase database, MongoSettings settings, IConfiguration config)
    {
        _ctx = new MongoContext(database);
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
        var seedCount = _config.GetValue<int>("Seed:Count", 12);
        
        // Create Owners first
        var owners = Enumerable.Range(1, seedCount / 2 + 1).Select(i => new OwnerDocument
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = GetRandomOwnerName(random),
            Address = GetRandomOwnerAddress(random),
            Photo = $"https://picsum.photos/id/{100 + i}/300/300",
            Birthday = DateTime.UtcNow.AddYears(-random.Next(25, 65)).AddDays(-random.Next(0, 365))
        }).ToList();

        await _ctx.Owners.InsertManyAsync(owners, cancellationToken: ct);

        // Create Properties
        var docs = Enumerable.Range(1, seedCount).Select(i => new PropertyDocument
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = owners[random.Next(owners.Count)].Id, // Reference existing owner
            Name = $"Property {i}",
            Address = i % 2 == 0 ? "Beverly Hills, CA" : "Manhattan, NY",
            Price = random.Next(100_000, 5_000_000),
            OperationType = i % 2 == 0 ? "sale" : "rent",
            Description = GetRandomDescription(random),
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

        // Create PropertyTraces 
        var traces = new List<PropertyTraceDocument>();
        foreach (var property in docs)
        {
            var traceCount = random.Next(1, 4); // 1-3 traces per property
            for (var k = 0; k < traceCount; k++)
            {
                traces.Add(new PropertyTraceDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    PropertyId = property.Id,
                    DateSale = DateTime.UtcNow.AddDays(-random.Next(30, 1095)), // Last 3 years
                    Name = GetRandomTraceName(random),
                    Value = property.Price * (decimal)(0.8 + random.NextDouble() * 0.4), // ±20% of current price
                    Tax = property.Price * (decimal)(0.01 + random.NextDouble() * 0.02) // 1-3% tax
                });
            }
        }
        await _ctx.PropertyTraces.InsertManyAsync(traces, cancellationToken: ct);
    }

    private static string GetRandomOwnerName(Random random)
    {
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Jessica", "William", "Ashley" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
    }

    private static string GetRandomOwnerAddress(Random random)
    {
        var addresses = new[]
        {
            "123 Oak Street, Los Angeles, CA",
            "456 Pine Avenue, New York, NY", 
            "789 Elm Drive, Miami, FL",
            "321 Maple Lane, Chicago, IL",
            "654 Cedar Road, Houston, TX",
            "987 Birch Boulevard, Phoenix, AZ"
        };
        return addresses[random.Next(addresses.Length)];
    }

    private static string GetRandomDescription(Random random)
    {
        var descriptions = new[]
        {
            "Luxurious modern home with stunning city views",
            "Charming family residence in quiet neighborhood", 
            "Contemporary apartment with premium finishes",
            "Elegant townhouse with spacious layout",
            "Beautiful villa with private garden",
            "Sophisticated penthouse with panoramic views"
        };
        return descriptions[random.Next(descriptions.Length)];
    }

    private static string GetRandomTraceName(Random random)
    {
        var traceNames = new[]
        {
            "Initial Sale",
            "Property Transfer", 
            "Ownership Change",
            "Estate Transfer",
            "Investment Purchase",
            "Family Inheritance"
        };
        return traceNames[random.Next(traceNames.Length)];
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

        // Owners indexes
        var ownerIdx = new CreateIndexModel<OwnerDocument>(Builders<OwnerDocument>.IndexKeys
            .Ascending(x => x.Name));
        await _ctx.Owners.Indexes.CreateOneAsync(ownerIdx, cancellationToken: ct);

        // PropertyTraces indexes  
        var traceIdx = new CreateIndexModel<PropertyTraceDocument>(Builders<PropertyTraceDocument>.IndexKeys
            .Ascending(x => x.PropertyId)
            .Descending(x => x.DateSale));
        await _ctx.PropertyTraces.Indexes.CreateOneAsync(traceIdx, cancellationToken: ct);
    }
}
