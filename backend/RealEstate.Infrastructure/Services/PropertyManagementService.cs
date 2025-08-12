using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class PropertyManagementService
{
    private readonly MongoContext _ctx;
    private readonly ILogger<PropertyManagementService> _logger;

    public PropertyManagementService(MongoContext ctx, ILogger<PropertyManagementService> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    // This service is not currently implementing any interface
    // It can be used for property management operations when needed
}
