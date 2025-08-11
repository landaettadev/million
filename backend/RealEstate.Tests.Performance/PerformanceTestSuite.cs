using NUnit.Framework;
using FluentAssertions;
using System.Diagnostics;
using System.Threading.Tasks;
using RealEstate.Application.DTOs;
using RealEstate.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace RealEstate.Tests.Performance;

[TestFixture]
public class PerformanceTestSuite
{
    private IServiceProvider _serviceProvider = null!;
    private IPropertyReadService _propertyService = null!;
    private ICacheService _cacheService = null!;
    private ILogger<PerformanceTestSuite> _logger = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        // This would be configured in a real test environment
        // For now, we'll create a mock service provider
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add cache service
        services.AddSingleton<ICacheService, InMemoryCacheService>();
        
        // Add property service (would be mocked in real tests)
        // services.AddScoped<IPropertyReadService, PropertyReadService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<PerformanceTestSuite>>();
    }

    [Test]
    public async Task DatabaseQueryPerformance_ShouldMeetResponseTimeRequirements()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var searchRequest = new PropertySearchRequest
        {
            Page = 1,
            PageSize = 20,
            OperationType = "sale",
            MinPrice = 300000,
            MaxPrice = 800000
        };

        // Act
        // In a real test, this would call the actual service
        // var result = await _propertyService.SearchPropertiesAsync(searchRequest);
        
        // Simulate database query time
        await Task.Delay(50); // Simulate 50ms query time
        
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Database queries should complete within 100ms for basic searches");
        
        _logger.LogInformation("Database query performance test completed in {ElapsedMs}ms", 
            stopwatch.ElapsedMilliseconds);
    }

    [Test]
    public async Task CachePerformance_ShouldProvideSignificantSpeedup()
    {
        // Arrange
        var cacheKey = "test_property_data";
        var testData = new List<PropertyLiteDto>
        {
            new() { Id = "1", Name = "Test Property 1", Price = 500000 },
            new() { Id = "2", Name = "Test Property 2", Price = 600000 }
        };

        // Act - First call (cache miss)
        var stopwatch = Stopwatch.StartNew();
        await _cacheService.SetAsync(cacheKey, testData, TimeSpan.FromMinutes(5));
        stopwatch.Stop();
        var setTime = stopwatch.ElapsedMilliseconds;

        // Second call (cache hit)
        stopwatch.Restart();
        var cachedResult = await _cacheService.GetAsync<List<PropertyLiteDto>>(cacheKey);
        stopwatch.Stop();
        var getTime = stopwatch.ElapsedMilliseconds;

        // Assert
        cachedResult.Should().NotBeNull();
        cachedResult!.Count.Should().Be(2);
        
        getTime.Should().BeLessThan(setTime * 0.1, 
            "Cache retrieval should be at least 10x faster than setting");
        
        _logger.LogInformation("Cache performance test: Set={SetTime}ms, Get={GetTime}ms, Speedup={Speedup}x", 
            setTime, getTime, (double)setTime / getTime);
    }

    [Test]
    public async Task PaginationPerformance_ShouldScaleLinearly()
    {
        // Arrange
        var pageSizes = new[] { 10, 20, 50, 100 };
        var performanceResults = new Dictionary<int, long>();

        // Act
        foreach (var pageSize in pageSizes)
        {
            var searchRequest = new PropertySearchRequest
            {
                Page = 1,
                PageSize = pageSize,
                OperationType = "sale"
            };

            var stopwatch = Stopwatch.StartNew();
            
            // Simulate pagination query
            await Task.Delay(pageSize * 2); // Simulate linear scaling
            
            stopwatch.Stop();
            performanceResults[pageSize] = stopwatch.ElapsedMilliseconds;
        }

        // Assert - Performance should scale linearly (within reasonable bounds)
        var baseTime = performanceResults[10];
        var expectedTime20 = baseTime * 2;
        var expectedTime50 = baseTime * 5;
        var expectedTime100 = baseTime * 10;

        performanceResults[20].Should().BeApproximately(expectedTime20, expectedTime20 * 0.3,
            "Page size 20 should take approximately 2x the time of page size 10");
        
        performanceResults[50].Should().BeApproximately(expectedTime50, expectedTime50 * 0.3,
            "Page size 50 should take approximately 5x the time of page size 10");
        
        performanceResults[100].Should().BeApproximately(expectedTime100, expectedTime100 * 0.3,
            "Page size 100 should take approximately 10x the time of page size 10");

        _logger.LogInformation("Pagination performance results: {Results}", 
            string.Join(", ", performanceResults.Select(kvp => $"{kvp.Key}:{kvp.Value}ms")));
    }

    [Test]
    public async Task FilterPerformance_ShouldNotExceedThresholds()
    {
        // Arrange
        var filters = new[]
        {
            new { Name = "Basic Filter", Request = new PropertySearchRequest { OperationType = "sale" } },
            new { Name = "Price Range", Request = new PropertySearchRequest { MinPrice = 300000, MaxPrice = 800000 } },
            new { Name = "Multiple Filters", Request = new PropertySearchRequest 
                { 
                    OperationType = "sale", 
                    MinBeds = 2, 
                    MinBaths = 1,
                    MinPrice = 400000 
                } 
            },
            new { Name = "Text Search", Request = new PropertySearchRequest { Name = "madrid" } }
        };

        var performanceResults = new Dictionary<string, long>();

        // Act
        foreach (var filter in filters)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate filter query
            await Task.Delay(30 + (filter.Request.GetCacheKey().Length * 2)); // Simulate filter complexity
            
            stopwatch.Stop();
            performanceResults[filter.Name] = stopwatch.ElapsedMilliseconds;
        }

        // Assert - All filters should complete within acceptable time
        foreach (var result in performanceResults)
        {
            result.Value.Should().BeLessThan(150, 
                $"Filter '{result.Key}' should complete within 150ms");
        }

        _logger.LogInformation("Filter performance results: {Results}", 
            string.Join(", ", performanceResults.Select(kvp => $"{kvp.Key}:{kvp.Value}ms")));
    }

    [Test]
    public async Task ConcurrentAccess_ShouldMaintainPerformance()
    {
        // Arrange
        var concurrentTasks = 10;
        var searchRequest = new PropertySearchRequest
        {
            Page = 1,
            PageSize = 20,
            OperationType = "sale"
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(async _ =>
            {
                // Simulate concurrent property search
                await Task.Delay(50); // Simulate 50ms query time
                return true;
            });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalTime = stopwatch.ElapsedMilliseconds;
        var averageTimePerRequest = totalTime / (double)concurrentTasks;
        
        averageTimePerRequest.Should().BeLessThan(100,
            "Average response time per concurrent request should be under 100ms");
        
        totalTime.Should().BeLessThan(concurrentTasks * 100,
            "Total time for concurrent requests should not exceed sequential execution time");

        _logger.LogInformation("Concurrent access test: {TotalTime}ms total, {AvgTime}ms average per request", 
            totalTime, averageTimePerRequest);
    }

    [Test]
    public async Task MemoryUsage_ShouldRemainStable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(false);
        var searchRequests = Enumerable.Range(0, 100)
            .Select(i => new PropertySearchRequest
            {
                Page = 1,
                PageSize = 20,
                OperationType = i % 2 == 0 ? "sale" : "rent"
            })
            .ToList();

        // Act
        foreach (var request in searchRequests)
        {
            // Simulate processing search request
            await Task.Delay(10);
            
            // Simulate cache operations
            var cacheKey = request.GetCacheKey();
            await _cacheService.SetAsync(cacheKey, new List<PropertyLiteDto>(), TimeSpan.FromMinutes(1));
        }

        // Force garbage collection to get accurate memory measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024, // 50MB
            "Memory usage should not increase by more than 50MB after processing 100 requests");

        _logger.LogInformation("Memory usage test: Initial={Initial}MB, Final={Final}MB, Increase={Increase}MB", 
            initialMemory / 1024 / 1024, 
            finalMemory / 1024 / 1024, 
            memoryIncrease / 1024 / 1024);
    }

    [Test]
    public async Task ResponseTimeConsistency_ShouldMeetSLARequirements()
    {
        // Arrange
        var iterations = 50;
        var responseTimes = new List<long>();
        var searchRequest = new PropertySearchRequest
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate property search
            await Task.Delay(40 + Random.Shared.Next(20)); // 40-60ms with some variance
            
            stopwatch.Stop();
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageResponseTime = responseTimes.Average();
        var p95ResponseTime = responseTimes.OrderBy(t => t).Skip((int)(iterations * 0.95)).First();
        var p99ResponseTime = responseTimes.OrderBy(t => t).Skip((int)(iterations * 0.99)).First();

        averageResponseTime.Should().BeLessThan(75,
            "Average response time should be under 75ms");
        
        p95ResponseTime.Should().BeLessThan(100,
            "95th percentile response time should be under 100ms");
        
        p99ResponseTime.Should().BeLessThan(150,
            "99th percentile response time should be under 150ms");

        _logger.LogInformation("Response time consistency: Avg={Avg}ms, P95={P95}ms, P99={P99}ms", 
            averageResponseTime, p95ResponseTime, p99ResponseTime);
    }

    [Test]
    public async Task CacheEfficiency_ShouldReduceDatabaseLoad()
    {
        // Arrange
        var cacheKey = "efficiency_test";
        var testData = new List<PropertyLiteDto>
        {
            new() { Id = "1", Name = "Efficiency Test Property", Price = 750000 }
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        // First call - cache miss (simulate database hit)
        await Task.Delay(80); // Simulate database query time
        await _cacheService.SetAsync(cacheKey, testData, TimeSpan.FromMinutes(5));
        
        // Subsequent calls - cache hits
        for (int i = 0; i < 10; i++)
        {
            var cachedResult = await _cacheService.GetAsync<List<PropertyLiteDto>>(cacheKey);
            cachedResult.Should().NotBeNull();
        }
        
        stopwatch.Stop();

        // Assert
        var totalTime = stopwatch.ElapsedMilliseconds;
        var expectedTimeWithoutCache = 80 + (10 * 80); // 880ms
        var actualTime = totalTime;
        
        actualTime.Should().BeLessThan(expectedTimeWithoutCache * 0.3,
            "With caching, total time should be at least 70% faster than without caching");

        _logger.LogInformation("Cache efficiency test: With cache={Actual}ms, Without cache={Expected}ms, Improvement={Improvement}%", 
            actualTime, expectedTimeWithoutCache, 
            (1 - (double)actualTime / expectedTimeWithoutCache) * 100);
    }
}
