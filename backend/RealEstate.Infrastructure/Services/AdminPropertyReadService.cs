using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class AdminPropertyReadService
{
    private readonly MongoContext _ctx;
    private readonly ILogger<AdminPropertyReadService> _logger;

    public AdminPropertyReadService(MongoContext ctx, ILogger<AdminPropertyReadService> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    // This service is not currently implementing any interface
    // It can be used for admin property operations when needed
}
