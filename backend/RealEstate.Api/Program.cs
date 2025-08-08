using Microsoft.OpenApi.Models;
using RealEstate.Application;
using RealEstate.Api.Endpoints;
using RealEstate.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins.Length > 0 ? allowedOrigins : new[] { "http://localhost:3000", "http://localhost:3001" })
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RealEstate API",
        Version = "v1",
        Description = "Minimal API for properties search and details"
    });
});

// Application + Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPropertyReadService, PropertyReadService>();

var app = builder.Build();

// Global error handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var envelope = new
        {
            traceId = context.TraceIdentifier,
            error = "Unexpected error",
            details = app.Environment.IsDevelopment() ? ex.Message : null
        };
        await context.Response.WriteAsJsonAsync(envelope);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");

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
