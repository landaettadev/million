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
        services.AddScoped<IImageStorageService, AzureBlobStorageService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        
        // Seeders
        services.AddScoped<MongoSeeder>();
        
        return services;
    }
}
