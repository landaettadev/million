using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http;
using FluentAssertions;
using RealEstate.Infrastructure;
using RealEstate.Tests.Performance.Infrastructure;
using Xunit;

namespace RealEstate.Tests.Performance.LoadTests;

public class PropertyApiLoadTests
{
    [Fact]
    public async Task LoadTest_PropertyListEndpoint_CanHandle100ConcurrentUsers()
    {
        // Arrange
        using var factory = new PerformanceTestWebAppFactory();
        await factory.InitializeAsync();
        
        var httpClient = factory.CreateClient();
        
        // Seed test data
        await SeedPerformanceDataAsync(factory);
        
        // Simple load test without NBomber for now
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Simulate 100 concurrent requests
        for (int i = 0; i < 100; i++)
        {
            var page = Random.Shared.Next(1, 5);
            var pageSize = Random.Shared.Next(10, 30);
            
            tasks.Add(httpClient.GetAsync($"/api/properties?page={page}&pageSize={pageSize}"));
        }
        
        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assertions
        responses.Length.Should().Be(100);
        responses.All(r => r.IsSuccessStatusCode).Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
        
        await factory.DisposeAsync();
    }
    
    [Fact]
    public async Task StressTest_PropertyApi_UnderHighLoad()
    {
        // Arrange
        using var factory = new PerformanceTestWebAppFactory();
        await factory.InitializeAsync();
        
        var httpClient = factory.CreateClient();
        await SeedPerformanceDataAsync(factory);
        
        // Simple stress test without NBomber
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task<HttpResponseMessage>>();
        
        var endpoints = new[]
        {
            "/api/properties",
            "/api/properties?pageSize=50",
            "/api/properties?name=Property&minPrice=100000",
            "/api/properties?operationType=sale"
        };
        
        // Simulate 200 concurrent requests with different endpoints
        for (int i = 0; i < 200; i++)
        {
            var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
            tasks.Add(httpClient.GetAsync(endpoint));
        }
        
        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Stress test assertions
        responses.Length.Should().Be(200);
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var errorRate = (double)(responses.Length - successCount) / responses.Length * 100;
        
        errorRate.Should().BeLessThan(5); // Less than 5% error rate
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
        
        await factory.DisposeAsync();
    }
    
    private static async Task SeedPerformanceDataAsync(PerformanceTestWebAppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoContext>();
        
        // Clear existing data
        await context.Properties.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyDocument>.Empty);
        await context.PropertyImages.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyImageDocument>.Empty);
        await context.Owners.DeleteManyAsync(MongoDB.Driver.FilterDefinition<OwnerDocument>.Empty);
        
        // Create large dataset for performance testing
        var owners = CreateTestOwners(50);
        var properties = CreateTestProperties(owners, 500);
        var images = CreateTestImages(properties);
        
        await context.Owners.InsertManyAsync(owners);
        await context.Properties.InsertManyAsync(properties);
        await context.PropertyImages.InsertManyAsync(images);
    }
    
    private static List<OwnerDocument> CreateTestOwners(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new OwnerDocument
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = $"Performance Owner {i}",
                Address = $"Performance Address {i}",
                Photo = $"https://example.com/perf{i}.jpg",
                Birthday = DateTime.UtcNow.AddYears(-Random.Shared.Next(25, 65))
            })
            .ToList();
    }
    
    private static List<PropertyDocument> CreateTestProperties(List<OwnerDocument> owners, int count)
    {
        var random = new Random(42);
        var descriptions = new[]
        {
            "Luxurious modern home", "Charming family residence", "Contemporary apartment",
            "Elegant townhouse", "Beautiful villa", "Sophisticated penthouse"
        };
        
        return Enumerable.Range(1, count)
            .Select(i => new PropertyDocument
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                OwnerId = owners[random.Next(owners.Count)].Id,
                Name = $"Performance Property {i}",
                Address = i % 3 == 0 ? "Beverly Hills, CA" : i % 3 == 1 ? "Manhattan, NY" : "Miami, FL",
                Price = random.Next(100_000, 5_000_000),
                OperationType = i % 2 == 0 ? "sale" : "rent",
                Description = descriptions[random.Next(descriptions.Length)],
                Beds = random.Next(1, 6),
                Baths = random.Next(1, 4),
                HalfBaths = random.Next(0, 2),
                Sqft = random.Next(800, 8000)
            })
            .ToList();
    }
    
    private static List<PropertyImageDocument> CreateTestImages(List<PropertyDocument> properties)
    {
        var images = new List<PropertyImageDocument>();
        var random = new Random(42);
        
        foreach (var property in properties)
        {
            var imageCount = random.Next(2, 4); // Fewer images for performance
            for (int i = 0; i < imageCount; i++)
            {
                images.Add(new PropertyImageDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    PropertyId = property.Id,
                    File = $"https://picsum.photos/id/{300 + random.Next(1, 300)}/1600/1000",
                    Enabled = i == 0,
                    Order = i + 1
                });
            }
        }
        
        return images;
    }
}
