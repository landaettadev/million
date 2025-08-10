using System.Linq.Expressions;
using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminPropertyReadService : IAdminPropertyReadService
{
    private readonly MongoContext _ctx;

    public AdminPropertyReadService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResult<AdminPropertyDto>> SearchAsync(AdminPropertySearchQuery query, CancellationToken ct = default)
    {
        var filter = BuildFilter(query);
        var sort = BuildSort(query.SortBy, query.SortDirection);
        var skip = (query.Page - 1) * query.PageSize;

        var properties = await _ctx.Properties
            .Aggregate()
            .Match(filter)
            .Lookup<PropertyDocument, OwnerDocument, PropertyWithOwner>(
                _ctx.Owners,
                p => p.OwnerId,
                o => o.Id,
                p => p.Owner)
            .Unwind<PropertyWithOwner, PropertyWithOwner>(p => p.Owner)
            .Project(p => new AdminPropertyDto(
                p.Id,
                p.OwnerId,
                p.Owner.Name,
                p.Name,
                p.Address,
                p.Price,
                Enum.Parse<OperationType>(p.OperationType, true),
                p.Description,
                p.Beds,
                p.Baths,
                p.HalfBaths,
                p.Sqft,
                p.CreatedAt,
                p.UpdatedAt,
                p.IsDeleted
            ))
            .Sort(BuildSort(query.SortBy, query.SortDirection))
            .Skip(skip)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        var totalCount = await _ctx.Properties.CountDocumentsAsync(filter, cancellationToken: ct);

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
            Enum.TryParse<OperationType>(doc.OperationType, true, out var op) ? op : OperationType.Sale,
            doc.Description,
            doc.Beds,
            doc.Baths,
            doc.HalfBaths,
            doc.Sqft,
            images.Select(i => new AdminPropertyImageDto(i.Id, i.File, i.Enabled, i.Order, i.CreatedAt)).ToList(),
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
