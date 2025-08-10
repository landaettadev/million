using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using RealEstate.Infrastructure;

namespace RealEstate.Api.Health;

public sealed class MongoHealthCheck : IHealthCheck
{
    private readonly MongoContext _mongo;

    public MongoHealthCheck(MongoContext mongo)
    {
        _mongo = mongo;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Run a lightweight ping command
            var result = await _mongo.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB ping successful");
        }
        catch (System.Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB ping failed", ex);
        }
    }
}


