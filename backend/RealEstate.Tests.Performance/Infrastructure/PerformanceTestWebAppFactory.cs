using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RealEstate.Infrastructure;
using Testcontainers.MongoDb;
using Xunit;

namespace RealEstate.Tests.Performance.Infrastructure;

public class PerformanceTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .WithUsername("admin")
        .WithPassword("password123")
        .WithPortBinding(27017, true)
        .WithCommand("--quiet") // Reduce MongoDB logging
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = _mongoContainer.GetConnectionString(),
                ["MongoDb:Database"] = "realestate_performance",
                ["Seed:Enabled"] = "false",
                ["Swagger:Enabled"] = "false",
                ["Logging:LogLevel:Default"] = "Warning", // Reduce logging noise
                ["Logging:LogLevel:Microsoft"] = "Error",
                ["Logging:LogLevel:System"] = "Error"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing MongoContext registration
            services.RemoveAll(typeof(MongoContext));
            
            // Add performance-optimized MongoContext
            services.AddSingleton<MongoContext>(serviceProvider =>
            {
                var settings = new MongoSettings
                {
                    ConnectionString = _mongoContainer.GetConnectionString(),
                    Database = "realestate_performance"
                };
                return new MongoContext(settings);
            });

            // Minimal logging for performance tests
            services.AddLogging(builder => 
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Error);
            });
        });

        builder.UseEnvironment("Performance");
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
        
        // Warm up the application
        var client = CreateClient();
        try
        {
            await client.GetAsync("/api/properties?pageSize=1");
        }
        catch
        {
            // Ignore warm-up errors
        }
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.StopAsync();
        await base.DisposeAsync();
    }
}
