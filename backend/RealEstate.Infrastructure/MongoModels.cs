using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RealEstate.Application;

namespace RealEstate.Infrastructure;

public sealed class MongoSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public CollectionNames CollectionNames { get; set; } = new();
}

public sealed class CollectionNames
{
    public string Properties { get; set; } = "properties";
    public string PropertyImages { get; set; } = "propertyImages";
    public string PropertyTraces { get; set; } = "propertyTraces";
    public string Owners { get; set; } = "owners";
}

public sealed class MongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<PropertyDocument> Properties { get; }
    public IMongoCollection<PropertyImageDocument> PropertyImages { get; }

    public MongoContext(MongoSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.Database);
        Properties = Database.GetCollection<PropertyDocument>(settings.CollectionNames.Properties);
        PropertyImages = Database.GetCollection<PropertyImageDocument>(settings.CollectionNames.PropertyImages);
    }
}

[BsonIgnoreExtraElements]
public sealed class PropertyDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string OwnerId { get; set; } = default!;

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string OperationType { get; set; } = string.Empty; // "sale" | "rent"

    public int? Beds { get; set; }
    public int? Baths { get; set; }
    public int? HalfBaths { get; set; }
    public int? Sqft { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class PropertyImageDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; } = default!;

    public string File { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public sealed class PropertyRepository : IPropertyRepository
{
    private readonly MongoContext _ctx;

    public PropertyRepository(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResult<PropertyLiteDto>> SearchAsync(SearchPropertiesQuery query, CancellationToken ct = default)
    {
        var fb = Builders<PropertyDocument>.Filter;
        var filters = new List<FilterDefinition<PropertyDocument>>();

        if (!string.IsNullOrWhiteSpace(query.Name))
            filters.Add(fb.Regex(x => x.Name, new BsonRegularExpression(query.Name, "i")));
        if (!string.IsNullOrWhiteSpace(query.Address))
            filters.Add(fb.Regex(x => x.Address, new BsonRegularExpression(query.Address, "i")));
        if (query.MinPrice.HasValue)
            filters.Add(fb.Gte(x => x.Price, query.MinPrice.Value));
        if (query.MaxPrice.HasValue)
            filters.Add(fb.Lte(x => x.Price, query.MaxPrice.Value));
        if (query.OperationType.HasValue)
            filters.Add(fb.Eq(x => x.OperationType, query.OperationType.Value == OperationType.Sale ? "sale" : "rent"));

        var filter = filters.Count > 0 ? fb.And(filters) : FilterDefinition<PropertyDocument>.Empty;

        var skip = (query.Page - 1) * query.PageSize;
        var total = await _ctx.Properties.CountDocumentsAsync(filter, cancellationToken: ct);

        // Proyección lite con primera imagen habilitada
        var pipeline = _ctx.Properties
            .Find(filter)
            .SortBy(x => x.Price)
            .Skip(skip)
            .Limit(query.PageSize)
            .ToEnumerable(ct)
            .Select(doc => (doc, image: _ctx.PropertyImages
                .Find(i => i.PropertyId == doc.Id && i.Enabled)
                .FirstOrDefault(ct)?.File));

        var items = pipeline.Select(x => new PropertyLiteDto(
            Id: x.doc.Id,
            IdOwner: x.doc.OwnerId,
            Name: x.doc.Name,
            Address: x.doc.Address,
            Price: x.doc.Price,
            Image: x.image,
            OperationType: string.Equals(x.doc.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent,
            Beds: x.doc.Beds,
            Baths: x.doc.Baths,
            HalfBaths: x.doc.HalfBaths,
            Sqft: x.doc.Sqft
        )).ToList();

        return new PagedResult<PropertyLiteDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<PropertyDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _ctx.Properties.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (doc is null) return null;

        var images = await _ctx.PropertyImages
            .Find(i => i.PropertyId == id && i.Enabled)
            .Project(i => i.File)
            .ToListAsync(ct);

        return new PropertyDetailDto(
            Id: doc.Id,
            IdOwner: doc.OwnerId,
            Name: doc.Name,
            Address: doc.Address,
            Price: doc.Price,
            Images: images,
            OperationType: string.Equals(doc.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent,
            Beds: doc.Beds,
            Baths: doc.Baths,
            HalfBaths: doc.HalfBaths,
            Sqft: doc.Sqft
        );
    }
}
