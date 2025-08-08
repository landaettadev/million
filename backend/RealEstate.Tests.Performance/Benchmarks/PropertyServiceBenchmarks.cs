using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RealEstate.Application;
using RealEstate.Infrastructure;
using Testcontainers.MongoDb;

namespace RealEstate.Tests.Performance.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class PropertyServiceBenchmarks
{
    private MongoDbContainer _mongoContainer = null!;
    private IPropertyReadService _propertyService = null!;
    private MongoContext _mongoContext = null!;
    private string _existingPropertyId = null!;
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Start MongoDB container
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithUsername("admin")
            .WithPassword("password123")
            .Build();
            
        await _mongoContainer.StartAsync();
        
        // Setup services
        var services = new ServiceCollection();
        
        services.AddSingleton<MongoContext>(_ => new MongoContext(new MongoSettings
        {
            ConnectionString = _mongoContainer.GetConnectionString(),
            Database = "realestate_benchmark"
        }));
        
        services.AddSingleton<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IPropertyReadService, PropertyReadService>();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Error));
        
        var serviceProvider = services.BuildServiceProvider();
        _mongoContext = serviceProvider.GetRequiredService<MongoContext>();
        _propertyService = serviceProvider.GetRequiredService<IPropertyReadService>();
        
        // Seed data for benchmarks
        await SeedBenchmarkDataAsync();
    }
    
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _mongoContainer.StopAsync();
    }
    
    private async Task SeedBenchmarkDataAsync()
    {
        // Clear existing data
        await _mongoContext.Properties.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyDocument>.Empty);
        await _mongoContext.PropertyImages.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyImageDocument>.Empty);
        await _mongoContext.Owners.DeleteManyAsync(MongoDB.Driver.FilterDefinition<OwnerDocument>.Empty);
        
        // Create test data
        var owners = CreateTestOwners(100);
        var properties = CreateTestProperties(owners, 1000);
        var images = CreateTestImages(properties);
        
        await _mongoContext.Owners.InsertManyAsync(owners);
        await _mongoContext.Properties.InsertManyAsync(properties);
        await _mongoContext.PropertyImages.InsertManyAsync(images);
        
        _existingPropertyId = properties.First().Id;
    }
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Search")]
    public async Task<PagedResult<PropertyLiteDto>> SearchProperties_NoFilters_Page1()
    {
        var query = new SearchPropertiesQuery(Page: 1, PageSize: 20);
        return await _propertyService.SearchAsync(query);
    }
    
    [Benchmark]
    [BenchmarkCategory("Search")]
    public async Task<PagedResult<PropertyLiteDto>> SearchProperties_WithNameFilter()
    {
        var query = new SearchPropertiesQuery(Name: "Property", Page: 1, PageSize: 20);
        return await _propertyService.SearchAsync(query);
    }
    
    [Benchmark]
    [BenchmarkCategory("Search")]
    public async Task<PagedResult<PropertyLiteDto>> SearchProperties_WithPriceFilter()
    {
        var query = new SearchPropertiesQuery(MinPrice: 100000, MaxPrice: 500000, Page: 1, PageSize: 20);
        return await _propertyService.SearchAsync(query);
    }
    
    [Benchmark]
    [BenchmarkCategory("Search")]
    public async Task<PagedResult<PropertyLiteDto>> SearchProperties_LargePage()
    {
        var query = new SearchPropertiesQuery(Page: 1, PageSize: 50);
        return await _propertyService.SearchAsync(query);
    }
    
    [Benchmark]
    [BenchmarkCategory("Detail")]
    public async Task<PropertyDetailDto?> GetPropertyById()
    {
        return await _propertyService.GetByIdAsync(_existingPropertyId);
    }
    
    [Params(1, 5, 10)]
    public int ConcurrentRequests { get; set; }
    
    [Benchmark]
    [BenchmarkCategory("Concurrent")]
    public async Task ConcurrentPropertyRequests()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => _propertyService.GetByIdAsync(_existingPropertyId))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }
    
    private static List<OwnerDocument> CreateTestOwners(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new OwnerDocument
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = $"Owner {i}",
                Address = $"Address {i}",
                Photo = $"https://example.com/photo{i}.jpg",
                Birthday = DateTime.UtcNow.AddYears(-30 - (i % 40))
            })
            .ToList();
    }
    
    private static List<PropertyDocument> CreateTestProperties(List<OwnerDocument> owners, int count)
    {
        var random = new Random(42);
        return Enumerable.Range(1, count)
            .Select(i => new PropertyDocument
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                OwnerId = owners[random.Next(owners.Count)].Id,
                Name = $"Property {i}",
                Address = i % 2 == 0 ? "Beverly Hills, CA" : "Manhattan, NY",
                Price = random.Next(100_000, 5_000_000),
                OperationType = i % 2 == 0 ? "sale" : "rent",
                Description = $"Description for property {i}",
                Beds = random.Next(2, 6),
                Baths = random.Next(1, 4),
                HalfBaths = random.Next(0, 2),
                Sqft = random.Next(900, 6000)
            })
            .ToList();
    }
    
    private static List<PropertyImageDocument> CreateTestImages(List<PropertyDocument> properties)
    {
        var images = new List<PropertyImageDocument>();
        var random = new Random(42);
        
        foreach (var property in properties)
        {
            var imageCount = random.Next(2, 5);
            for (int i = 0; i < imageCount; i++)
            {
                images.Add(new PropertyImageDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    PropertyId = property.Id,
                    File = $"https://picsum.photos/id/{200 + random.Next(1, 500)}/1600/1000",
                    Enabled = i == 0,
                    Order = i + 1
                });
            }
        }
        
        return images;
    }
}
