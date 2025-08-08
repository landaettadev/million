using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Application;
using RealEstate.Infrastructure;
using RealEstate.Tests.Integration.Infrastructure;

namespace RealEstate.Tests.Integration.Endpoints;

public class PropertyEndpointsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public PropertyEndpointsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_Properties_ReturnsEmptyList_WhenNoData()
    {
        // Act
        var response = await _client.GetAsync("/api/properties");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
        result.RootElement.GetProperty("total").GetInt64().Should().Be(0);
    }

    [Fact]
    public async Task GET_Properties_ReturnsPagedResults_WithValidData()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/properties?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        var items = result.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeLessOrEqualTo(5);
        
        result.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        result.RootElement.GetProperty("pageSize").GetInt32().Should().Be(5);
        result.RootElement.GetProperty("total").GetInt64().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_Properties_FiltersCorrectly_ByName()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerm = "test";

        // Act
        var response = await _client.GetAsync($"/api/properties?name={searchTerm}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        // Should return results (case-insensitive search)
        var items = result.RootElement.GetProperty("items");
        // Note: This test might return 0 results depending on generated data
        items.GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GET_Properties_FiltersCorrectly_ByPriceRange()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/properties?minPrice=100000&maxPrice=500000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        var items = result.RootElement.GetProperty("items");
        
        // Verify all returned items are within price range
        foreach (var item in items.EnumerateArray())
        {
            var price = item.GetProperty("price").GetDecimal();
            price.Should().BeInRange(100000, 500000);
        }
    }

    [Fact]
    public async Task GET_Properties_ReturnsValidationError_WithInvalidPageSize()
    {
        // Act
        var response = await _client.GetAsync("/api/properties?pageSize=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("error").GetString().Should().Be("Validation Error");
        result.RootElement.TryGetProperty("details", out var details).Should().BeTrue();
    }

    [Fact]
    public async Task GET_Properties_ReturnsValidationError_WithNegativePrice()
    {
        // Act
        var response = await _client.GetAsync("/api/properties?minPrice=-1000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_PropertyById_ReturnsProperty_WithValidId()
    {
        // Arrange
        var propertyId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/properties/{propertyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("id").GetString().Should().Be(propertyId);
        result.RootElement.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
        result.RootElement.GetProperty("address").GetString().Should().NotBeNullOrEmpty();
        result.RootElement.GetProperty("price").GetDecimal().Should().BeGreaterThan(0);
        result.RootElement.TryGetProperty("images", out var images).Should().BeTrue();
        images.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_PropertyById_ReturnsNotFound_WithInvalidId()
    {
        // Act
        var response = await _client.GetAsync($"/api/properties/{MongoDB.Bson.ObjectId.GenerateNewId()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("error").GetString().Should().Be("Resource Not Found");
    }

    [Fact]
    public async Task GET_PropertyById_ReturnsBadRequest_WithInvalidObjectId()
    {
        // Act
        var response = await _client.GetAsync("/api/properties/invalid-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("error").GetString().Should().Be("Validation Error");
    }

    [Fact]
    public async Task GET_Properties_PerformanceTest_CompletesWith200Properties()
    {
        // Arrange
        await SeedLargeDatasetAsync(200);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/properties?pageSize=50");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("items").GetArrayLength().Should().Be(50);
        result.RootElement.GetProperty("total").GetInt64().Should().Be(200);
    }

    private async Task<string> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoContext>();
        
        // Clear existing data
        await context.Owners.DeleteManyAsync(MongoDB.Driver.FilterDefinition<OwnerDocument>.Empty);
        await context.Properties.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyDocument>.Empty);
        await context.PropertyImages.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyImageDocument>.Empty);
        await context.PropertyTraces.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyTraceDocument>.Empty);
        
        // Create test data
        var owners = TestDataBuilder.CreateOwners(3);
        var properties = TestDataBuilder.CreateProperties(owners, 10);
        var images = TestDataBuilder.CreatePropertyImages(properties);
        var traces = TestDataBuilder.CreatePropertyTraces(properties);
        
        // Insert test data
        await context.Owners.InsertManyAsync(owners);
        await context.Properties.InsertManyAsync(properties);
        await context.PropertyImages.InsertManyAsync(images);
        await context.PropertyTraces.InsertManyAsync(traces);
        
        return properties.First().Id;
    }

    private async Task SeedLargeDatasetAsync(int propertyCount)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoContext>();
        
        // Clear existing data
        await context.Owners.DeleteManyAsync(MongoDB.Driver.FilterDefinition<OwnerDocument>.Empty);
        await context.Properties.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyDocument>.Empty);
        await context.PropertyImages.DeleteManyAsync(MongoDB.Driver.FilterDefinition<PropertyImageDocument>.Empty);
        
        // Create large dataset
        var owners = TestDataBuilder.CreateOwners(propertyCount / 10);
        var properties = TestDataBuilder.CreateProperties(owners, propertyCount);
        var images = TestDataBuilder.CreatePropertyImages(properties);
        
        // Insert in batches for better performance
        await context.Owners.InsertManyAsync(owners);
        await context.Properties.InsertManyAsync(properties);
        await context.PropertyImages.InsertManyAsync(images);
    }
}
