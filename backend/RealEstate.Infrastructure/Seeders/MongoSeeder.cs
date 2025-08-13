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

        // Locked dataset mode: load deterministic JSON so every clone gets the same data
        var locked = _config.GetValue<bool>("Seed:Locked");
        if (locked)
        {
            await RunLockedDatasetAsync(ct);
            return;
        }

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

        var random = new Random(42);
        var seedCount = _config.GetValue<int>("Seed:Count", 12);
        
        // Create Owners first
        var owners = Enumerable.Range(1, seedCount / 2 + 1).Select(i => new OwnerDocument
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = GetRandomOwnerName(random),
            Address = GetRandomOwnerAddress(random),
            Photo = null,
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
                    File = string.Empty,
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

        // Admin user seeding done earlier to avoid early return preventing it
    }

    private async Task RunLockedDatasetAsync(CancellationToken ct)
    {
        var datasetPath = _config.GetValue<string>("Seed:DatasetPath") ?? Path.Combine("backend", "seed", "locked");
        // Fallback when running from backend project dir
        if (!Directory.Exists(datasetPath))
        {
            datasetPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "seed", "locked");
        }

        // Minimal schema for dataset files
        var ownersPath = Path.Combine(datasetPath, "owners.json");
        var propertiesPath = Path.Combine(datasetPath, "properties.json");
        var imagesPath = Path.Combine(datasetPath, "propertyImages.json");
        var videosPath = Path.Combine(datasetPath, "propertyVideos.json");

        var owners = File.Exists(ownersPath)
            ? System.Text.Json.JsonSerializer.Deserialize<List<SimpleOwner>>(await File.ReadAllTextAsync(ownersPath, ct)) ?? new()
            : new();
        var props = File.Exists(propertiesPath)
            ? System.Text.Json.JsonSerializer.Deserialize<List<SimpleProperty>>(await File.ReadAllTextAsync(propertiesPath, ct)) ?? new()
            : new();
        var images = File.Exists(imagesPath)
            ? System.Text.Json.JsonSerializer.Deserialize<List<SimpleImage>>(await File.ReadAllTextAsync(imagesPath, ct)) ?? new()
            : new();
        var videos = File.Exists(videosPath)
            ? System.Text.Json.JsonSerializer.Deserialize<List<SimpleVideo>>(await File.ReadAllTextAsync(videosPath, ct)) ?? new()
            : new();

        // Clear collections only if explicitly requested
        var reset = _config.GetValue<bool>("Seed:ResetCollections");
        if (reset)
        {
            await _ctx.Properties.DeleteManyAsync(_ => true, ct);
            await _ctx.PropertyImages.DeleteManyAsync(_ => true, ct);
            await _ctx.PropertyTraces.DeleteManyAsync(_ => true, ct);
            await _ctx.Owners.DeleteManyAsync(_ => true, ct);
        }

        // Insert owners
        var ownerIdByIndex = new Dictionary<int, string>();
        for (var i = 0; i < owners.Count; i++)
        {
            var o = owners[i];
            var doc = new OwnerDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = o.Name,
                Address = o.Address ?? string.Empty,
                Photo = null,
                Birthday = o.Birthday,
                CreatedAt = DateTime.UtcNow
            };
            await _ctx.Owners.InsertOneAsync(doc, cancellationToken: ct);
            ownerIdByIndex[i] = doc.Id;
        }

        // Insert properties
        var propertyIdByIndex = new Dictionary<int, string>();
        for (var i = 0; i < props.Count; i++)
        {
            var p = props[i];
            var doc = new PropertyDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                OwnerId = (p.OwnerIndex.HasValue && ownerIdByIndex.TryGetValue(p.OwnerIndex.Value, out var oid)) ? oid : null,
                Name = p.Name,
                Address = p.Address,
                Price = p.Price,
                OperationType = p.OperationType?.ToLowerInvariant() == "rent" ? "rent" : "sale",
                Description = p.Description,
                Beds = p.Beds,
                Baths = p.Baths,
                HalfBaths = p.HalfBaths,
                Sqft = p.Sqft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                IsFeatured = p.IsFeatured,
                VideoUrl = null
            };
            await _ctx.Properties.InsertOneAsync(doc, cancellationToken: ct);
            propertyIdByIndex[i] = doc.Id;
        }

        // Insert images
        foreach (var img in images)
        {
            if (!img.PropertyIndex.HasValue) continue;
            if (!propertyIdByIndex.TryGetValue(img.PropertyIndex.Value, out var pid)) continue;
            var doc = new PropertyImageDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PropertyId = pid,
                File = img.BlobName,
                ThumbnailFile = null,
                Enabled = img.Enabled,
                Order = img.Order,
                FileSize = img.FileSize,
                ContentType = string.IsNullOrWhiteSpace(img.ContentType) ? "image/jpeg" : img.ContentType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _ctx.PropertyImages.InsertOneAsync(doc, cancellationToken: ct);
        }

        // Insert videos (as property.VideoUrl)
        foreach (var v in videos)
        {
            if (!v.PropertyIndex.HasValue) continue;
            if (!propertyIdByIndex.TryGetValue(v.PropertyIndex.Value, out var pid)) continue;
            var update = Builders<PropertyDocument>.Update
                .Set(p => p.VideoUrl, v.Url)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);
            await _ctx.Properties.UpdateOneAsync(p => p.Id == pid, update, cancellationToken: ct);
        }
    }

    private sealed record SimpleOwner(string Name, string? Address = null, DateTime? Birthday = null);
    private sealed record SimpleProperty(string Name, string Address, decimal Price, string OperationType, int? Beds = null, int? Baths = null, int? HalfBaths = null, int? Sqft = null, string? Description = null, bool IsFeatured = false, int? OwnerIndex = null);
    private sealed record SimpleImage(int? PropertyIndex, string BlobName, bool Enabled = true, int Order = 1, long FileSize = 0, string ContentType = "image/jpeg");
    private sealed record SimpleVideo(int? PropertyIndex, string Url);

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
