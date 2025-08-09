using FluentValidation;
using Microsoft.OpenApi.Models;
using RealEstate.Api.Endpoints;
using RealEstate.Api.Middleware;
using RealEstate.Application;
using RealEstate.Application.Validators;
using RealEstate.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
builder.Configuration.AddEnvironmentVariables();

// CORS - Support both config file and environment variables
var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")?.Split(',')
                     ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                     ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        var origins = allowedOrigins.Length > 0 ? allowedOrigins : new[] { "http://localhost:3000", "http://localhost:3001" };
        
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("X-Total-Count", "X-Page", "X-PageSize"); // For pagination headers
    });
    
    // Add a more restrictive policy for production
    options.AddPolicy("production", policy =>
        policy.WithOrigins(allowedOrigins.Where(o => o.StartsWith("https://")).ToArray())
              .WithHeaders("Content-Type", "Authorization")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .AllowCredentials());
});

// Swagger - Only enable in Development/Staging
var swaggerEnabled = Environment.GetEnvironmentVariable("SWAGGER_ENABLED")?.ToLower() == "true"
                     || builder.Configuration.GetValue<bool>("Swagger:Enabled", false)
                     || builder.Environment.IsDevelopment();

if (swaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "RealEstate API",
            Version = "v1",
            Description = "Minimal API for properties search and details",
            Contact = new OpenApiContact
            {
                Name = "Real Estate API Team",
                Email = "api-team@realestate.com"
            }
        });
        
        // Add security definition for future use
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT token"
        });
    });
}

// FluentValidation
builder.Services.AddScoped<IValidator<SearchPropertiesQuery>, SearchPropertiesQueryValidator>();
builder.Services.AddScoped<IValidator<string>, PropertyIdValidator>();

// Application + Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPropertyReadService, PropertyReadService>();

var app = builder.Build();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    
    await next();
});

// Global error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RealEstate API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "RealEstate API Documentation";
    });
}

// Use appropriate CORS policy based on environment
var corsPolicy = app.Environment.IsProduction() ? "production" : "frontend";
app.UseCors(corsPolicy);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Map endpoints
app.MapPropertyEndpoints();

// Seed on start
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<MongoSeeder>();
    await seeder.RunAsync();
}

app.Run();

// Make Program accessible for testing
namespace RealEstate.Api
{
    public partial class Program { }
}
