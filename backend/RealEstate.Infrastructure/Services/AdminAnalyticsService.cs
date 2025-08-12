using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RealEstate.Application;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly MongoContext _ctx;

    public AdminAnalyticsService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(CancellationToken ct = default)
    {
        var totalProperties = await _ctx.Properties.CountDocumentsAsync(x => !x.IsDeleted, cancellationToken: ct);
        var totalOwners = await _ctx.Owners.CountDocumentsAsync(x => !x.IsDeleted, cancellationToken: ct);
        
        // Simple analytics for now
        return new AnalyticsDto(
            (int)totalProperties,
            (int)totalOwners,
            0m, // TotalRevenue
            0m, // MonthlyRevenue
            0m  // YearlyRevenue
        );
    }
}
