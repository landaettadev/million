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
        // Indexes (skip in testing or when explicitly disabled)
        var skipIndexes =
            _config.GetValue<bool>("Seed:SkipIndexes") ||
            string.Equals(Environment.GetEnvironmentVariable("SKIP_INDEXES"), "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Testing", StringComparison.OrdinalIgnoreCase);

        if (!skipIndexes)
        {
            await EnsureIndexesAsync(ct);
        }

        var seedEnabled = _config.GetValue<bool>("Seed:Enabled");
        var insertIfEmpty = _config.GetValue<bool>("Seed:InsertIfEmpty");
        if (!seedEnabled) return;

        // Seed Admin user (idempotent) - do this BEFORE early return based on properties count
        var seedAdminEmail = _config.GetValue<string>("Admin:Seed:Email");
        var seedAdminPassword = _config.GetValue<string>("Admin:Seed:Password");
        if (!string.IsNullOrWhiteSpace(seedAdminEmail) && !string.IsNullOrWhiteSpace(seedAdminPassword))
        {
            var existingAdmin = await _ctx.AdminUsers.Find(u => u.Email == seedAdminEmail).FirstOrDefaultAsync(ct);
            if (existingAdmin is null)
            {
                var admin = new AdminUserDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    Email = seedAdminEmail,
                    Name = "Administrator",
                    Role = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedAdminPassword)
                };
                await _ctx.AdminUsers.InsertOneAsync(admin, cancellationToken: ct);
            }
            else
            {
                var update = Builders<AdminUserDocument>.Update
                    .Set(x => x.Role, "Admin")
                    .Set(x => x.Name, string.IsNullOrWhiteSpace(existingAdmin.Name) ? "Administrator" : existingAdmin.Name)
                    .Set(x => x.PasswordHash, BCrypt.Net.BCrypt.HashPassword(seedAdminPassword));
                await _ctx.AdminUsers.UpdateOneAsync(x => x.Id == existingAdmin.Id, update, cancellationToken: ct);
            }
        }

        var count = await _ctx.Properties.CountDocumentsAsync(FilterDefinition<PropertyDocument>.Empty, cancellationToken: ct);
        if (count > 0 && insertIfEmpty) return;

        // Deterministic demo data (no picsum, real-like content). Videos local, images from Azure Blob by file name.
        var ownerDocs = new List<OwnerDocument>
        {
            new() { Id = ObjectId.GenerateNewId().ToString(), Name = "María González", Address = "Calle Mayor 123, Madrid" },
            new() { Id = ObjectId.GenerateNewId().ToString(), Name = "Carlos Rodríguez", Address = "Diagonal 456, Barcelona" },
            new() { Id = ObjectId.GenerateNewId().ToString(), Name = "Ana Martínez", Address = "Malvarrosa Beach 789, Valencia" },
            new() { Id = ObjectId.GenerateNewId().ToString(), Name = "Luis Fernández", Address = "Calle Sierpes 321, Sevilla" }
        };
        await _ctx.Owners.InsertManyAsync(ownerDocs, cancellationToken: ct);

        // Map: code -> (property, owner)
        var propertiesByCode = new Dictionary<string, PropertyDocument>
        {
            ["MAD001"] = new PropertyDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OwnerId = ownerDocs[0].Id,
                Name = "Luxury Penthouse Madrid",
                Address = "Paseo de la Castellana 123, Madrid",
                Price = 850000,
                OperationType = "sale",
                Description = "Exclusive penthouse with panoramic views of Madrid",
                Beds = 4,
                Baths = 3,
                HalfBaths = 1,
                Sqft = 3500,
                IsFeatured = true,
                VideoUrl = "/videos/lujosa1.mp4"
            },
            ["BCN001"] = new PropertyDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OwnerId = ownerDocs[1].Id,
                Name = "Modern Apartment Barcelona",
                Address = "Diagonal 456, Barcelona",
                Price = 450000,
                OperationType = "sale",
                Description = "Contemporary apartment in the heart of Barcelona",
                Beds = 3,
                Baths = 2,
                HalfBaths = 0,
                Sqft = 2200,
                IsFeatured = true,
                VideoUrl = "/videos/lujosa2.mp4"
            },
            ["VAL001"] = new PropertyDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OwnerId = ownerDocs[2].Id,
                Name = "Beach House Valencia",
                Address = "Malvarrosa Beach 789, Valencia",
                Price = 650000,
                OperationType = "sale",
                Description = "Beautiful beachfront property with private access",
                Beds = 5,
                Baths = 4,
                HalfBaths = 1,
                Sqft = 4200
            },
            ["SEV001"] = new PropertyDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OwnerId = ownerDocs[3].Id,
                Name = "Downtown Loft Sevilla",
                Address = "Calle Sierpes 321, Sevilla",
                Price = 280000,
                OperationType = "sale",
                Description = "Charming loft in the historic center of Sevilla",
                Beds = 2,
                Baths = 1,
                HalfBaths = 0,
                Sqft = 1500
            }
        };

        await _ctx.Properties.InsertManyAsync(propertiesByCode.Values, cancellationToken: ct);

        var images = new List<PropertyImageDocument>();
        void AddImages(string code, params string[] files)
        {
            var pid = propertiesByCode[code].Id;
            for (var i = 0; i < files.Length; i++)
            {
                images.Add(new PropertyImageDocument
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    PropertyId = pid,
                    File = files[i], // blob file name (no URL). Service composes URL.
                    Enabled = i == 0,
                    Order = i + 1
                });
            }
        }

        AddImages("MAD001", "madrid-penthouse-1.jpg", "madrid-penthouse-2.jpg", "madrid-penthouse-3.jpg");
        AddImages("BCN001", "barcelona-apartment-1.jpg", "barcelona-apartment-2.jpg");
        AddImages("VAL001", "valencia-beach-1.jpg", "valencia-beach-2.jpg", "valencia-beach-3.jpg");
        AddImages("SEV001", "sevilla-loft-1.jpg", "sevilla-loft-2.jpg");

        await _ctx.PropertyImages.InsertManyAsync(images, cancellationToken: ct);

        // Optional: minimal traces
        var traces = new List<PropertyTraceDocument>();
        foreach (var p in propertiesByCode.Values)
        {
            traces.Add(new PropertyTraceDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PropertyId = p.Id,
                DateSale = DateTime.UtcNow.AddDays(-120),
                Name = "Initial Listing",
                Value = p.Price,
                Tax = p.Price * 0.02m
            });
        }
        await _ctx.PropertyTraces.InsertManyAsync(traces, cancellationToken: ct);

        // Admin user seeding done earlier to avoid early return preventing it
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
