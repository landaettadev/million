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
    public string AdminUsers { get; set; } = "adminUsers";
    public string RefreshTokens { get; set; } = "refreshTokens";
    public string TokenBlacklist { get; set; } = "tokenBlacklist";
}

public sealed class MongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<PropertyDocument> Properties { get; }
    public IMongoCollection<PropertyImageDocument> PropertyImages { get; }
    public IMongoCollection<OwnerDocument> Owners { get; }
    public IMongoCollection<PropertyTraceDocument> PropertyTraces { get; }
    public IMongoCollection<AdminUserDocument> AdminUsers { get; }
    public IMongoCollection<RefreshTokenDocument> RefreshTokens { get; }
    public IMongoCollection<TokenBlacklistDocument> TokenBlacklist { get; }

    public MongoContext(MongoSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.Database);
        Properties = Database.GetCollection<PropertyDocument>(settings.CollectionNames.Properties);
        PropertyImages = Database.GetCollection<PropertyImageDocument>(settings.CollectionNames.PropertyImages);
        Owners = Database.GetCollection<OwnerDocument>(settings.CollectionNames.Owners);
        PropertyTraces = Database.GetCollection<PropertyTraceDocument>(settings.CollectionNames.PropertyTraces);
        AdminUsers = Database.GetCollection<AdminUserDocument>(settings.CollectionNames.AdminUsers);
        RefreshTokens = Database.GetCollection<RefreshTokenDocument>(settings.CollectionNames.RefreshTokens);
        TokenBlacklist = Database.GetCollection<TokenBlacklistDocument>(settings.CollectionNames.TokenBlacklist);
    }

    public MongoContext(IMongoDatabase database)
    {
        Database = database;
        Properties = Database.GetCollection<PropertyDocument>("properties");
        PropertyImages = Database.GetCollection<PropertyImageDocument>("propertyImages");
        Owners = Database.GetCollection<OwnerDocument>("owners");
        PropertyTraces = Database.GetCollection<PropertyTraceDocument>("propertyTraces");
        AdminUsers = Database.GetCollection<AdminUserDocument>("adminUsers");
        RefreshTokens = Database.GetCollection<RefreshTokenDocument>("refreshTokens");
        TokenBlacklist = Database.GetCollection<TokenBlacklistDocument>("tokenBlacklist");
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
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
    public string? ThumbnailFile { get; set; }
    public bool Enabled { get; set; }
    public int Order { get; set; } = 1;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}

[BsonIgnoreExtraElements]
public sealed class OwnerDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Photo { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}

[BsonIgnoreExtraElements]
public sealed class PropertyTraceDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string PropertyId { get; set; } = default!;

    public DateTime DateSale { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Tax { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class AdminUserDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public string PasswordHash { get; set; } = string.Empty;
    public List<RefreshTokenDocument> RefreshTokens { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class RefreshTokenDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public string? RevokedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class TokenBlacklistDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty; // "logout", "security", "expired"
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
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
        var filters = new List<FilterDefinition<PropertyDocument>>
        {
            fb.Eq(x => x.IsDeleted, false)
        };

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

        var filter = filters.Count > 0 ? fb.And(filters) : fb.Eq(x => x.IsDeleted, false);

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
                .Find(i => i.PropertyId == property.Id && i.Enabled && !i.IsDeleted)
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
        var doc = await _ctx.Properties.Find(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync(ct);
        if (doc is null) return null;

        var images = await _ctx.PropertyImages
            .Find(i => i.PropertyId == id && i.Enabled && !i.IsDeleted)
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
