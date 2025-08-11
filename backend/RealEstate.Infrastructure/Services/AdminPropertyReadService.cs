using System.Linq.Expressions;
using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminPropertyReadService : IAdminPropertyReadService
{
    private readonly MongoContext _ctx;
    private readonly IImageStorageService _imageStorageService;

    public AdminPropertyReadService(MongoContext ctx, IImageStorageService imageStorageService)
    {
        _ctx = ctx;
        _imageStorageService = imageStorageService;
    }

    public async Task<PagedResult<AdminPropertyDto>> SearchAsync(AdminPropertySearchQuery query, CancellationToken ct = default)
    {
        var filter = BuildFilter(query);
        var skip = (query.Page - 1) * query.PageSize;

        // Use a simpler approach: fetch properties first, then get owners separately
        var propertyDocs = await _ctx.Properties
            .Find(filter)
            .Skip(skip)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        var totalCount = await _ctx.Properties.CountDocumentsAsync(filter, cancellationToken: ct);

        // Get all unique owner IDs from the properties
        var ownerIds = propertyDocs.Select(p => p.OwnerId).Distinct().ToList();
        
        // Fetch all owners in one query
        var owners = await _ctx.Owners
            .Find(o => ownerIds.Contains(o.Id) && !o.IsDeleted)
            .ToListAsync(ct);
        
        var ownerDict = owners.ToDictionary(o => o.Id, o => o);

        // Build the result DTOs
        var properties = propertyDocs.Select(p => 
        {
            var owner = ownerDict.GetValueOrDefault(p.OwnerId);
            return new AdminPropertyDto(
                p.Id,
                p.OwnerId,
                owner?.Name ?? "Unknown Owner",
                p.Name,
                p.Address,
                p.Price,
                string.Equals(p.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent,
                p.Description,
                p.Beds,
                p.Baths,
                p.HalfBaths,
                p.Sqft,
                p.CreatedAt,
                p.UpdatedAt,
                p.IsDeleted
            );
        }).ToList();

        return new PagedResult<AdminPropertyDto>(
            properties,
            query.Page,
            query.PageSize,
            totalCount
        );
    }

    public async Task<AdminPropertyDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _ctx.Properties.Find(p => p.Id == id && !p.IsDeleted).FirstOrDefaultAsync(ct);
        if (doc is null) return null;

        var owner = await _ctx.Owners.Find(o => o.Id == doc.OwnerId && !o.IsDeleted).FirstOrDefaultAsync(ct);
        var images = await _ctx.PropertyImages
            .Find(i => i.PropertyId == id && !i.IsDeleted)
            .SortBy(i => i.Order)
            .ToListAsync(ct);

        return new AdminPropertyDetailDto(
            doc.Id,
            doc.OwnerId,
            owner?.Name ?? string.Empty,
            owner?.Address ?? string.Empty,
            doc.Name,
            doc.Address,
            doc.Price,
            string.Equals(doc.OperationType, "sale", StringComparison.OrdinalIgnoreCase) ? OperationType.Sale : OperationType.Rent,
            doc.Description,
            doc.Beds,
            doc.Baths,
            doc.HalfBaths,
            doc.Sqft,
            await Task.WhenAll(images.Select(async i => new AdminPropertyImageDto(
                i.Id, 
                i.PropertyId, 
                i.File, 
                await _imageStorageService.GetImageUrlAsync(i.File, ct), 
                i.Enabled, 
                i.Order, 
                i.File, // Use file name as filename for now
                i.FileSize, 
                i.ContentType, 
                i.CreatedAt
            ))),
            doc.CreatedAt,
            doc.UpdatedAt,
            doc.IsDeleted
        );
    }

    public async Task<long> GetTotalCountAsync(CancellationToken ct = default)
    {
        return await _ctx.Properties.CountDocumentsAsync(p => !p.IsDeleted, cancellationToken: ct);
    }

    private static FilterDefinition<PropertyDocument> BuildFilter(AdminPropertySearchQuery query)
    {
        var filters = new List<FilterDefinition<PropertyDocument>>
        {
            Builders<PropertyDocument>.Filter.Eq(p => p.IsDeleted, false)
        };

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchFilter = Builders<PropertyDocument>.Filter.Or(
                Builders<PropertyDocument>.Filter.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(query.SearchTerm, "i")),
                Builders<PropertyDocument>.Filter.Regex(p => p.Address, new MongoDB.Bson.BsonRegularExpression(query.SearchTerm, "i")),
                Builders<PropertyDocument>.Filter.Regex(p => p.Description, new MongoDB.Bson.BsonRegularExpression(query.SearchTerm, "i"))
            );
            filters.Add(searchFilter);
        }

        if (!string.IsNullOrWhiteSpace(query.OwnerId))
        {
            filters.Add(Builders<PropertyDocument>.Filter.Eq(p => p.OwnerId, query.OwnerId));
        }

        if (query.MinPrice.HasValue)
        {
            filters.Add(Builders<PropertyDocument>.Filter.Gte(p => p.Price, query.MinPrice.Value));
        }

        if (query.MaxPrice.HasValue)
        {
            filters.Add(Builders<PropertyDocument>.Filter.Lte(p => p.Price, query.MaxPrice.Value));
        }

        if (query.OperationType.HasValue)
        {
            filters.Add(Builders<PropertyDocument>.Filter.Eq(p => p.OperationType, query.OperationType.Value.ToString()));
        }

        return Builders<PropertyDocument>.Filter.And(filters);
    }

    private static SortDefinition<AdminPropertyDto> BuildSort(string sortBy, string sortDirection)
    {
        var sort = sortDirection.ToLower() == "desc" 
            ? Builders<AdminPropertyDto>.Sort.Descending(sortBy) 
            : Builders<AdminPropertyDto>.Sort.Ascending(sortBy);
        return sort;
    }

    private sealed class PropertyWithOwner
    {
        public string Id { get; set; } = default!;
        public string OwnerId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;
        public decimal Price { get; set; }
        public string OperationType { get; set; } = default!;
        public string? Description { get; set; }
        public int? Beds { get; set; }
        public int? Baths { get; set; }
        public int? HalfBaths { get; set; }
        public int? Sqft { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public OwnerDocument Owner { get; set; } = default!;
    }

    private sealed class PropertyWithOwnerAndImages
    {
        public string Id { get; set; } = default!;
        public string OwnerId { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string OwnerAddress { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;
        public decimal Price { get; set; }
        public string OperationType { get; set; } = default!;
        public string? Description { get; set; }
        public int? Beds { get; set; }
        public int? Baths { get; set; }
        public int? HalfBaths { get; set; }
        public int? Sqft { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public List<PropertyImageDocument> Images { get; set; } = new();
    }
}
