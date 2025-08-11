using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminImageReadService : IAdminImageReadService
{
    private readonly MongoContext _ctx;

    public AdminImageReadService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResult<AdminImageDto>> SearchAsync(AdminImageSearchQuery query, CancellationToken ct = default)
    {
        var filter = BuildFilter(query);
        var skip = (query.Page - 1) * query.PageSize;

        var images = await _ctx.PropertyImages
            .Aggregate()
            .Match(filter)
            .Lookup<PropertyImageDocument, PropertyDocument, ImageWithProperty>(
                _ctx.Properties,
                i => i.PropertyId,
                p => p.Id,
                i => i.Property)
            .Unwind<ImageWithProperty, ImageWithProperty>(i => i.Property)
            .Project(i => new AdminImageDto(
                i.Id,
                i.PropertyId,
                i.Property.Name,
                i.File,
                i.Enabled,
                i.Order,
                i.CreatedAt,
                i.UpdatedAt,
                i.IsDeleted
            ))
            .Sort(BuildSort(query.SortBy, query.SortDirection))
            .Skip(skip)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        var totalCount = await _ctx.PropertyImages.CountDocumentsAsync(filter, cancellationToken: ct);

        return new PagedResult<AdminImageDto>(
            images,
            query.Page,
            query.PageSize,
            totalCount
        );
    }

    public async Task<AdminImageDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var image = await _ctx.PropertyImages
            .Aggregate()
            .Match(i => i.Id == id && !i.IsDeleted)
            .Lookup<PropertyImageDocument, PropertyDocument, ImageWithProperty>(
                _ctx.Properties,
                i => i.PropertyId,
                p => p.Id,
                i => i.Property)
            .Unwind<ImageWithProperty, ImageWithProperty>(i => i.Property)
            .Project(i => new AdminImageDetailDto(
                i.Id,
                i.PropertyId,
                i.Property.Name,
                i.Property.Address,
                i.File,
                i.Enabled,
                i.Order,
                i.FileSize,
                i.ContentType,
                i.CreatedAt,
                i.UpdatedAt,
                i.IsDeleted
            ))
            .FirstOrDefaultAsync(ct);

        return image;
    }

    public async Task<long> GetTotalCountAsync(CancellationToken ct = default)
    {
        return await _ctx.PropertyImages.CountDocumentsAsync(i => !i.IsDeleted, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<AdminImageDto>> GetByPropertyIdAsync(string propertyId, CancellationToken ct = default)
    {
        // Use a simpler approach that directly maps from PropertyImageDocument
        var images = await _ctx.PropertyImages
            .Find(i => i.PropertyId == propertyId && !i.IsDeleted)
            .SortBy(i => i.Order)
            .ToListAsync(ct);

        // Get property name for the images
        var property = await _ctx.Properties
            .Find(p => p.Id == propertyId)
            .Project(p => new { p.Name })
            .FirstOrDefaultAsync(ct);

        var propertyName = property?.Name ?? "Unknown Property";

        // Map to DTOs
        var imageDtos = images.Select(i => new AdminImageDto(
            i.Id,
            i.PropertyId,
            propertyName,
            i.File,
            i.Enabled,
            i.Order,
            i.CreatedAt,
            i.UpdatedAt,
            i.IsDeleted
        )).ToList();

        return imageDtos;
    }

    private static FilterDefinition<PropertyImageDocument> BuildFilter(AdminImageSearchQuery query)
    {
        var filters = new List<FilterDefinition<PropertyImageDocument>>
        {
            Builders<PropertyImageDocument>.Filter.Eq(i => i.IsDeleted, false)
        };

        if (!string.IsNullOrWhiteSpace(query.PropertyId))
        {
            filters.Add(Builders<PropertyImageDocument>.Filter.Eq(i => i.PropertyId, query.PropertyId));
        }

        if (query.Enabled.HasValue)
        {
            filters.Add(Builders<PropertyImageDocument>.Filter.Eq(i => i.Enabled, query.Enabled.Value));
        }

        return Builders<PropertyImageDocument>.Filter.And(filters);
    }

    private static SortDefinition<AdminImageDto> BuildSort(string sortBy, string sortDirection)
    {
        var sort = sortDirection.ToLower() == "desc" 
            ? Builders<AdminImageDto>.Sort.Descending(sortBy) 
            : Builders<AdminImageDto>.Sort.Ascending(sortBy);
        return sort;
    }

    private sealed class ImageWithProperty
    {
        public string Id { get; set; } = default!;
        public string PropertyId { get; set; } = default!;
        public string File { get; set; } = default!;
        public bool Enabled { get; set; }
        public int Order { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public PropertyDocument Property { get; set; } = default!;
    }
}
