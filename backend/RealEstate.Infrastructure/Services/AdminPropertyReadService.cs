using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminPropertyReadService : IAdminPropertyService
{
    private readonly MongoContext _ctx;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<AdminPropertyReadService> _logger;

    public AdminPropertyReadService(MongoContext ctx, IImageStorageService imageStorageService, ILogger<AdminPropertyReadService> logger)
    {
        _ctx = ctx;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    public async Task<PagedResult<AdminPropertyDto>> GetPropertiesAsync(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var filter = Builders<PropertyDocument>.Filter.Eq(x => x.IsDeleted, false);
        var skip = (page - 1) * pageSize;

        var docs = await _ctx.Properties
            .Find(filter)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(ct);

        var total = await _ctx.Properties.CountDocumentsAsync(filter, cancellationToken: ct);

        // Build owner name lookup to populate OwnerName in the response
        var ownerIds = docs.Select(d => d.OwnerId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var owners = ownerIds.Count == 0
            ? new List<(string Id, string Name)>()
            : (await _ctx.Owners
                .Find(o => ownerIds.Contains(o.Id))
                .Project(o => new { o.Id, o.Name })
                .ToListAsync(ct))
                .Select(o => (o.Id, o.Name))
                .ToList();

        var ownerNameById = owners.ToDictionary(o => o.Id, o => o.Name);

        var items = new List<AdminPropertyDto>();
        foreach (var p in docs)
        {
            var op = string.Equals(p.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent;
            var ownerName = (p.OwnerId != null && ownerNameById.TryGetValue(p.OwnerId, out var name)) ? name : string.Empty;

            items.Add(new AdminPropertyDto(
                Id: p.Id,
                OwnerId: p.OwnerId,
                OwnerName: ownerName,
                Name: p.Name,
                Address: p.Address,
                Price: p.Price,
                OperationType: op,
                CreatedAt: p.CreatedAt,
                IsDeleted: p.IsDeleted,
                Description: p.Description,
                Beds: p.Beds,
                Baths: p.Baths,
                HalfBaths: p.HalfBaths,
                Sqft: p.Sqft,
                UpdatedAt: p.UpdatedAt
            ));
        }

        return new PagedResult<AdminPropertyDto>(items, page, pageSize, total);
    }

    public async Task<AdminPropertyDto?> GetPropertyByIdAsync(string id, CancellationToken ct = default)
    {
        var p = await _ctx.Properties.Find(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync(ct);
        if (p == null) return null;

        var op = string.Equals(p.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent;
        return new AdminPropertyDto(
            Id: p.Id,
            OwnerId: p.OwnerId,
            OwnerName: string.Empty,
            Name: p.Name,
            Address: p.Address,
            Price: p.Price,
            OperationType: op,
            CreatedAt: p.CreatedAt,
            IsDeleted: p.IsDeleted,
            Description: p.Description,
            Beds: p.Beds,
            Baths: p.Baths,
            HalfBaths: p.HalfBaths,
            Sqft: p.Sqft,
            UpdatedAt: p.UpdatedAt
        );
    }

    public async Task<AdminPropertyDto> CreatePropertyAsync(CreatePropertyDto createDto, CancellationToken ct = default)
    {
        var doc = new PropertyDocument
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OwnerId = createDto.OwnerId,
            Name = createDto.Name,
            Address = createDto.Address,
            Price = createDto.Price,
            OperationType = createDto.OperationType == OperationType.Sale ? "sale" : "rent",
            Description = createDto.Description,
            Beds = createDto.Beds,
            Baths = createDto.Baths,
            HalfBaths = createDto.HalfBaths,
            Sqft = createDto.Sqft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            IsFeatured = false
        };

        await _ctx.Properties.InsertOneAsync(doc, cancellationToken: ct);

        var op2 = string.Equals(doc.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent;
        return new AdminPropertyDto(
            Id: doc.Id,
            OwnerId: doc.OwnerId,
            OwnerName: string.Empty,
            Name: doc.Name,
            Address: doc.Address,
            Price: doc.Price,
            OperationType: op2,
            CreatedAt: doc.CreatedAt,
            IsDeleted: doc.IsDeleted,
            Description: doc.Description,
            Beds: doc.Beds,
            Baths: doc.Baths,
            HalfBaths: doc.HalfBaths,
            Sqft: doc.Sqft,
            UpdatedAt: doc.UpdatedAt
        );
    }

    public async Task<AdminPropertyDto> UpdatePropertyAsync(string id, UpdatePropertyDto updateDto, CancellationToken ct = default)
    {
        var update = Builders<PropertyDocument>.Update
            .Set(p => p.Name, updateDto.Name)
            .Set(p => p.Address, updateDto.Address)
            .Set(p => p.Price, updateDto.Price)
            .Set(p => p.OperationType, updateDto.OperationType == OperationType.Sale ? "sale" : "rent")
            .Set(p => p.Description, updateDto.Description ?? null)
            .Set(p => p.Beds, updateDto.Beds)
            .Set(p => p.Baths, updateDto.Baths)
            .Set(p => p.HalfBaths, updateDto.HalfBaths)
            .Set(p => p.Sqft, updateDto.Sqft)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        await _ctx.Properties.UpdateOneAsync(p => p.Id == id, update, cancellationToken: ct);

        var updated = await GetPropertyByIdAsync(id, ct);
        if (updated == null) throw new InvalidOperationException($"Property {id} not found after update");
        return updated;
    }

    public async Task DeletePropertyAsync(string id, CancellationToken ct = default)
    {
        var update = Builders<PropertyDocument>.Update
            .Set(p => p.IsDeleted, true)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);
        await _ctx.Properties.UpdateOneAsync(p => p.Id == id, update, cancellationToken: ct);
    }
}
