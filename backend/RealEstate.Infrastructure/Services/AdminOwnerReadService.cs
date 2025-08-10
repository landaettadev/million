using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminOwnerReadService : IAdminOwnerReadService
{
    private readonly MongoContext _ctx;

    public AdminOwnerReadService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResult<AdminOwnerDto>> SearchAsync(AdminOwnerSearchQuery query, CancellationToken ct = default)
    {
        var filter = BuildFilter(query);
        var skip = (query.Page - 1) * query.PageSize;

        // Sort by OwnerDocument fields
        IFindFluent<OwnerDocument, OwnerDocument> cursor = _ctx.Owners.Find(filter);
        if (string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            cursor = query.SortBy switch
            {
                "Name" => cursor.SortByDescending(o => o.Name),
                "Address" => cursor.SortByDescending(o => o.Address),
                _ => cursor.SortByDescending(o => o.CreatedAt)
            };
        }
        else
        {
            cursor = query.SortBy switch
            {
                "Name" => cursor.SortBy(o => o.Name),
                "Address" => cursor.SortBy(o => o.Address),
                _ => cursor.SortBy(o => o.CreatedAt)
            };
        }

        var ownerDocs = await cursor.Skip(skip).Limit(query.PageSize).ToListAsync(ct);

        // Batch count properties per owner
        var ownerIds = ownerDocs.Select(o => o.Id).ToList();
        var props = await _ctx.Properties
            .Find(p => ownerIds.Contains(p.OwnerId) && !p.IsDeleted)
            .Project(p => new { p.OwnerId })
            .ToListAsync(ct);
        var counts = props.GroupBy(p => p.OwnerId).ToDictionary(g => g.Key, g => g.Count());

        var owners = ownerDocs.Select(o => new AdminOwnerDto(
            o.Id,
            o.Name,
            o.Address,
            o.Photo,
            o.Birthday,
            counts.TryGetValue(o.Id, out var c) ? c : 0,
            o.CreatedAt,
            o.UpdatedAt,
            o.IsDeleted
        )).ToList();

        var totalCount = await _ctx.Owners.CountDocumentsAsync(filter, cancellationToken: ct);

        return new PagedResult<AdminOwnerDto>(owners, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdminOwnerDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var ownerDoc = await _ctx.Owners.Find(o => o.Id == id && !o.IsDeleted).FirstOrDefaultAsync(ct);
        if (ownerDoc is null) return null;

        var ownerProps = await _ctx.Properties
            .Find(p => p.OwnerId == id && !p.IsDeleted)
            .ToListAsync(ct);

        var detail = new AdminOwnerDetailDto(
            ownerDoc.Id,
            ownerDoc.Name,
            ownerDoc.Address,
            ownerDoc.Photo,
            ownerDoc.Birthday,
            ownerProps.Select(p => new AdminOwnerPropertyDto(
                p.Id,
                p.Name,
                p.Address,
                p.Price,
                Enum.TryParse<OperationType>(p.OperationType, true, out var op) ? op : OperationType.Sale,
                p.CreatedAt
            )).ToList(),
            ownerDoc.CreatedAt,
            ownerDoc.UpdatedAt,
            ownerDoc.IsDeleted
        );

        return detail;
    }

    public async Task<long> GetTotalCountAsync(CancellationToken ct = default)
    {
        return await _ctx.Owners.CountDocumentsAsync(o => !o.IsDeleted, cancellationToken: ct);
    }

    private static FilterDefinition<OwnerDocument> BuildFilter(AdminOwnerSearchQuery query)
    {
        var filters = new List<FilterDefinition<OwnerDocument>>
        {
            Builders<OwnerDocument>.Filter.Eq(o => o.IsDeleted, false)
        };

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchFilter = Builders<OwnerDocument>.Filter.Or(
                Builders<OwnerDocument>.Filter.Regex(o => o.Name, new MongoDB.Bson.BsonRegularExpression(query.SearchTerm, "i")),
                Builders<OwnerDocument>.Filter.Regex(o => o.Address, new MongoDB.Bson.BsonRegularExpression(query.SearchTerm, "i"))
            );
            filters.Add(searchFilter);
        }

        return Builders<OwnerDocument>.Filter.And(filters);
    }

    private static SortDefinition<AdminOwnerDto> BuildSort(string sortBy, string sortDirection)
    {
        var sort = sortDirection.ToLower() == "desc" 
            ? Builders<AdminOwnerDto>.Sort.Descending(sortBy) 
            : Builders<AdminOwnerDto>.Sort.Ascending(sortBy);
        return sort;
    }

    private sealed class OwnerWithProperties
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string? Photo { get; set; }
        public DateTime? Birthday { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public List<PropertyDocument> Properties { get; set; } = new();
    }
}
