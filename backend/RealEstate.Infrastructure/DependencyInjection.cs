using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RealEstate.Application;
using RealEstate.Infrastructure.Services;

namespace RealEstate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB Configuration
        var mongoSettings = configuration.GetSection("MongoDb").Get<MongoSettings>();
        if (mongoSettings == null)
            throw new InvalidOperationException("MongoDB configuration is missing");
        
        services.AddSingleton(mongoSettings);
        services.AddSingleton<MongoContext>(provider => new MongoContext(mongoSettings));
        
        // Repositories
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        
        // Services
        // Decide image storage based on explicit flag. Default to local in Development unless overridden.
        var isDevelopment = string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);
        var useLocalImageStorage = configuration.GetValue<bool?>("UseLocalImageStorage")
            ?? isDevelopment; // default true in Development, false otherwise

        if (useLocalImageStorage)
        {
            services.AddScoped<IImageStorageService, DevelopmentImageStorageService>();
        }
        else
        {
            services.AddScoped<IImageStorageService, AzureBlobStorageService>();
        }
        // Use in-memory cache for tests/E2E when REDIS_DISABLED=true to avoid external dependency timeouts
        var redisDisabled = string.Equals(configuration["REDIS_DISABLED"], "true", StringComparison.OrdinalIgnoreCase);
        if (redisDisabled)
        {
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }
        else
        {
            services.AddScoped<ICacheService, RedisCacheService>();
        }

        // Admin services
        services.AddScoped<IAdminOwnerService, AdminOwnerService>();
        services.AddScoped<IAdminPropertyService, AdminPropertyReadService>();
        services.AddScoped<IAdminImageReadService, AdminImageReadService>();
        
        // Seeders
        services.AddScoped<MongoSeeder>();
        
        return services;
    }
}
