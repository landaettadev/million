using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Application;

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
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddSingleton<MongoSeeder>();

        return services;
    }
}
