using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RealEstate.Infrastructure;
using Testcontainers.MongoDb;

namespace RealEstate.Tests.Integration.Infrastructure;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .WithUsername("admin")
        .WithPassword("password123")
        .WithPortBinding(27017, true)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = _mongoContainer.GetConnectionString(),
                ["MongoDb:Database"] = "realestate_test",
                ["Seed:Enabled"] = "false", // Disable automatic seeding for tests
                ["Swagger:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing MongoContext registration
            services.RemoveAll(typeof(MongoContext));
            
            // Add test-specific MongoContext
            services.AddSingleton<MongoContext>(serviceProvider =>
            {
                var settings = new MongoSettings
                {
                    ConnectionString = _mongoContainer.GetConnectionString(),
                    Database = "realestate_test"
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
        await _mongoContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.StopAsync();
        await base.DisposeAsync();
    }
}
