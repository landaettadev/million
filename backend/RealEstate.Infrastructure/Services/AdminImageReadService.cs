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

    public async Task<List<PropertyImageDto>> GetPropertyImagesAsync(string propertyId, CancellationToken ct = default)
    {
        var images = await _ctx.PropertyImages
            .Find(i => i.PropertyId == propertyId && !i.IsDeleted)
            .SortBy(i => i.Order)
            .ToListAsync(ct);

        return images.Select(i => new PropertyImageDto(
            i.Id,
            i.File,
            i.File, // Using File as URL for now
            i.FileSize,
            i.ContentType,
            i.Order == 0, // First image is main
            i.Enabled,
            i.Order,
            i.CreatedAt
        )).ToList();
    }
}
