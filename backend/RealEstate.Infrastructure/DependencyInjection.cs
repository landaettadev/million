using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Application;
using RealEstate.Application.Interfaces;
using RealEstate.Infrastructure.Services;

namespace RealEstate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("MongoDb").Get<MongoSettings>() ?? new MongoSettings();
        services.AddSingleton(settings);

        services.AddSingleton<MongoContext>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IPropertyWriteService, PropertyWriteService>();
        services.AddScoped<IAdminPropertyReadService, Services.AdminPropertyReadService>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IOwnerWriteService, OwnerWriteService>();
        services.AddScoped<IAdminOwnerReadService, Services.AdminOwnerReadService>();
        services.AddScoped<IAdminImageReadService, Services.AdminImageReadService>();
        services.AddScoped<IImageWriteService, ImageWriteService>();
        services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
        
        // Image Storage Service - use Azure Blob Storage with development fallback
        services.AddScoped<AzureBlobStorageService>();
        services.AddScoped<IImageStorageService>(provider =>
        {
            var azureService = provider.GetRequiredService<AzureBlobStorageService>();
            return new DevelopmentImageStorageService(azureService);
        });

        services.AddSingleton<MongoSeeder>();

        return services;
    }
}
