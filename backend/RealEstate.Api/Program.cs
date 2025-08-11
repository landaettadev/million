using FluentValidation;
using Microsoft.OpenApi.Models;
using RealEstate.Api.Endpoints;
using RealEstate.Application;
using RealEstate.Application.Validators;
using RealEstate.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Serilog;
using Serilog.Context;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
// Global request body size limit (10 MB). Per-endpoint overrides can be applied via attributes.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessName()
    .Enrich.WithThreadName()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

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

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
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
builder.Services.AddScoped<IValidator<CreatePropertyDto>, CreatePropertyValidator>();
builder.Services.AddScoped<IValidator<UpdatePropertyDto>, UpdatePropertyValidator>();
builder.Services.AddScoped<IValidator<CreateOwnerDto>, CreateOwnerValidator>();
builder.Services.AddScoped<IValidator<UpdateOwnerDto>, UpdateOwnerValidator>();

// Application + Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPropertyReadService, PropertyReadService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPropertyWriteService, PropertyWriteService>();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Application Insights
var aiConnection = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(aiConnection))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Health checks
builder.Services.AddHealthChecks();

// JWT Auth
var jwtKey = builder.Configuration["JWT:KEY"];
if (!string.IsNullOrEmpty(jwtKey))
{
    var key = Encoding.UTF8.GetBytes(jwtKey);
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
    builder.Services.AddAuthorization();
}

var app = builder.Build();

// Health checks (ensure services registered before Build)
// Already registered via Infrastructure; mapping below

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' https: data:; media-src 'self' https:; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self' http: https:";
    
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    
    await next();
});

// Global error handling middleware
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var statusCode = exception switch
        {
            FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            traceId = context.TraceIdentifier,
            error = exception?.Message ?? "An unexpected error occurred",
            details = (string[]?)null,
            statusCode,
            timestamp = DateTime.UtcNow.ToString("O")
        };

        await context.Response.WriteAsJsonAsync(payload);
    });
});

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

// Serve static files from wwwroot folder (for images)
app.UseStaticFiles();

if (!string.IsNullOrEmpty(jwtKey))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Request logging with Serilog (adds TraceId/correlation info to logs)
app.UseSerilogRequestLogging();

// Correlation/TraceId header propagation
app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                        ?? ctx.TraceIdentifier;
    ctx.Response.Headers["X-Correlation-Id"] = correlationId;
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

app.MapHealthChecks("/health");

// Map endpoints
app.MapPropertyEndpoints();
// Map controllers (attribute-routed endpoints like admin/*)
app.MapControllers();

// Public GET caching headers disabled temporarily to investigate chunked encoding issue
// app.Use(async (ctx, next) =>
// {
//     if (ctx.Request.Method == "GET" && ctx.Request.Path.StartsWithSegments("/api/properties"))
//     {
//         ctx.Response.OnStarting(() =>
//         {
//             ctx.Response.Headers["Cache-Control"] = "public, max-age=60";
//             return Task.CompletedTask;
//         });
//     }
//     await next();
// });

// Seed on start (do not crash app if DB is unavailable in dev)
try
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<MongoSeeder>();
    await seeder.RunAsync();
}
catch (Exception ex)
{
    // Log and continue so the API can still start for health checks and static endpoints
    Log.Warning(ex, "Mongo seeding failed during startup. The API will start without seeded data.");
}

app.Run();

// Make Program accessible for testing
namespace RealEstate.Api
{
    public partial class Program { }
}
