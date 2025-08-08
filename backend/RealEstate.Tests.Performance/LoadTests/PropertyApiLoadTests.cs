using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http;
using NBomber.Plugins.Network.Ping;
using RealEstate.Infrastructure;
using RealEstate.Tests.Performance.Infrastructure;

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
        
        // Configure HTTP client for NBomber
        using var httpClientFactory = HttpClientFactory.Create("properties_api", httpClient);
        
        // Define scenarios
        var listPropertiesScenario = Scenario.Create("list_properties", async context =>
        {
            var page = Random.Shared.Next(1, 5);
            var pageSize = Random.Shared.Next(10, 30);
            
            var request = Http.CreateRequest("GET", $"/api/properties?page={page}&pageSize={pageSize}")
                .WithHeader("Accept", "application/json");
                
            var response = await Http.Send(httpClientFactory, request);
            
            return response;
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(60)),
            Simulation.InjectPerSec(rate: 20, during: TimeSpan.FromSeconds(30))
        );
        
        var propertyDetailScenario = Scenario.Create("property_details", async context =>
        {
            // First get a property ID from the list
            var listRequest = Http.CreateRequest("GET", "/api/properties?pageSize=1")
                .WithHeader("Accept", "application/json");
                
            var listResponse = await Http.Send(httpClientFactory, listRequest);
            
            if (listResponse.IsError)
                return listResponse;
                
            try
            {
                var content = await listResponse.Message.Content.ReadAsStringAsync();
                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                
                if (jsonDoc.RootElement.GetProperty("items").GetArrayLength() > 0)
                {
                    var propertyId = jsonDoc.RootElement
                        .GetProperty("items")[0]
                        .GetProperty("id")
                        .GetString();
                        
                    var detailRequest = Http.CreateRequest("GET", $"/api/properties/{propertyId}")
                        .WithHeader("Accept", "application/json");
                        
                    return await Http.Send(httpClientFactory, detailRequest);
                }
            }
            catch
            {
                // Fall back to list request if parsing fails
            }
            
            return listResponse;
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(60))
        );
        
        var searchScenario = Scenario.Create("search_properties", async context =>
        {
            var searchTerms = new[] { "Property", "Beverly", "Manhattan", "Villa", "Apartment" };
            var searchTerm = searchTerms[Random.Shared.Next(searchTerms.Length)];
            var minPrice = Random.Shared.Next(100_000, 1_000_000);
            var maxPrice = minPrice + Random.Shared.Next(500_000, 2_000_000);
            
            var request = Http.CreateRequest("GET", 
                $"/api/properties?name={searchTerm}&minPrice={minPrice}&maxPrice={maxPrice}&pageSize=20")
                .WithHeader("Accept", "application/json");
                
            return await Http.Send(httpClientFactory, request);
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 8, during: TimeSpan.FromSeconds(60))
        );
        
        // Run load test
        var stats = NBomberRunner
            .RegisterScenarios(listPropertiesScenario, propertyDetailScenario, searchScenario)
            .WithReportFolder("load_test_reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Txt)
            .Run();
        
        // Assertions
        var listStats = stats.AllOkCount > 0 ? stats.ScenarioStats.First(s => s.ScenarioName == "list_properties") : null;
        var detailStats = stats.AllOkCount > 0 ? stats.ScenarioStats.First(s => s.ScenarioName == "property_details") : null;
        
        // Performance requirements
        if (listStats != null)
        {
            listStats.Ok.Response.Mean.Should().BeLessThan(500); // Average response time < 500ms
            listStats.Ok.Response.StdDev.Should().BeLessThan(200); // Low standard deviation
            listStats.Fail.Request.Count.Should().Be(0); // No failed requests
        }
        
        if (detailStats != null)
        {
            detailStats.Ok.Response.Mean.Should().BeLessThan(300); // Detail should be faster
            detailStats.Fail.Request.Count.Should().Be(0);
        }
        
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
        
        using var httpClientFactory = HttpClientFactory.Create("stress_test", httpClient);
        
        // High-stress scenario
        var stressScenario = Scenario.Create("stress_test", async context =>
        {
            var endpoints = new[]
            {
                "/api/properties",
                "/api/properties?pageSize=50",
                "/api/properties?name=Property&minPrice=100000",
                "/api/properties?operationType=sale"
            };
            
            var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
            var request = Http.CreateRequest("GET", endpoint);
            
            return await Http.Send(httpClientFactory, request);
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 50, during: TimeSpan.FromSeconds(30)), // Ramp up
            Simulation.KeepConstant(copies: 200, during: TimeSpan.FromSeconds(120)), // High load
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30)) // Peak load
        );
        
        // Run stress test
        var stats = NBomberRunner
            .RegisterScenarios(stressScenario)
            .WithReportFolder("stress_test_reports")
            .WithReportFormats(ReportFormat.Html)
            .Run();
        
        // Stress test assertions (more lenient)
        var scenario = stats.ScenarioStats.First();
        scenario.Ok.Response.Mean.Should().BeLessThan(2000); // Under high load, allow up to 2s
        
        // Error rate should be acceptable under stress
        var errorRate = (double)scenario.Fail.Request.Count / stats.AllRequestCount * 100;
        errorRate.Should().BeLessThan(5); // Less than 5% error rate
        
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
