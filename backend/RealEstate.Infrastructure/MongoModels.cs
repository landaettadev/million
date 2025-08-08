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

    public MongoContext(IMongoDatabase database)
    {
        Database = database;
        Properties = Database.GetCollection<PropertyDocument>("properties");
        PropertyImages = Database.GetCollection<PropertyImageDocument>("propertyImages");
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
    public string? Description { get; set; }

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
    public int Order { get; set; } = 1;
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

        var properties = await _ctx.Properties
            .Find(filter)
            .SortBy(x => x.Price)
            .Skip(skip)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        var items = new List<PropertyLiteDto>();
        
        foreach (var property in properties)
        {
            var firstImage = await _ctx.PropertyImages
                .Find(i => i.PropertyId == property.Id && i.Enabled)
                .SortBy(i => i.Order)
                .Project(i => i.File)
                .FirstOrDefaultAsync(ct);

            items.Add(new PropertyLiteDto(
                Id: property.Id,
                IdOwner: property.OwnerId,
                Name: property.Name,
                Address: property.Address,
                Price: property.Price,
                Image: firstImage,
                OperationType: string.Equals(property.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent,
                Beds: property.Beds,
                Baths: property.Baths,
                HalfBaths: property.HalfBaths,
                Sqft: property.Sqft
            ));
        }

        return new PagedResult<PropertyLiteDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<PropertyDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _ctx.Properties.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (doc is null) return null;

        var images = await _ctx.PropertyImages
            .Find(i => i.PropertyId == id && i.Enabled)
            .SortBy(i => i.Order)
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
            Description: doc.Description,
            Beds: doc.Beds,
            Baths: doc.Baths,
            HalfBaths: doc.HalfBaths,
            Sqft: doc.Sqft
        );
    }
}
