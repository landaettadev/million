using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RealEstate.Infrastructure;
using Testcontainers.MongoDb;
using Xunit;

namespace RealEstate.Tests.Integration.Infrastructure;

public class IntegrationTestWebAppFactory : WebApplicationFactory<RealEstate.Api.Program>, Xunit.IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .WithUsername("admin")
        .WithPassword("password123")
        .WithPortBinding(0, 27017) // random host port → container 27017
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // ConnectionString will be provided via services after container starts
                ["MongoDb:Database"] = "realestate_test",
                ["Seed:Enabled"] = "false", // Disable automatic seeding for tests
                ["Swagger:Enabled"] = "false",
                ["REDIS_DISABLED"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing MongoContext registration
            services.RemoveAll(typeof(MongoContext));
            
            // Add test-specific MongoContext
            services.AddSingleton<MongoContext>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var configConn = configuration["MongoDb:ConnectionString"];

                // Default to local Mongo for integration tests unless explicitly disabled
                var useLocalEnv = Environment.GetEnvironmentVariable("USE_LOCAL_MONGO");
                var useLocal = string.IsNullOrWhiteSpace(useLocalEnv) || string.Equals(useLocalEnv, "true", StringComparison.OrdinalIgnoreCase);
                var connectionString = !string.IsNullOrWhiteSpace(configConn)
                    ? configConn
                    : useLocal
                        ? (Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? "mongodb://localhost:27017")
                        : _mongoContainer.GetConnectionString();

                var databaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE") ?? "realestate_test";

                var settings = new MongoSettings
                {
                    ConnectionString = connectionString,
                    Database = databaseName
                };
                return new MongoContext(settings);
            });

            // Suppress logging during tests
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        var useLocalEnv = Environment.GetEnvironmentVariable("USE_LOCAL_MONGO");
        var useLocal = string.IsNullOrWhiteSpace(useLocalEnv) || string.Equals(useLocalEnv, "true", StringComparison.OrdinalIgnoreCase);
        if (!useLocal)
        {
            await _mongoContainer.StartAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        var useLocalEnv = Environment.GetEnvironmentVariable("USE_LOCAL_MONGO");
        var useLocal = string.IsNullOrWhiteSpace(useLocalEnv) || string.Equals(useLocalEnv, "true", StringComparison.OrdinalIgnoreCase);
        if (!useLocal)
        {
            await _mongoContainer.StopAsync();
        }
        await base.DisposeAsync();
    }
}
