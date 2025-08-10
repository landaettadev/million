using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using RealEstate.Tests.Integration.Infrastructure;
using NUnit.Framework;
using RealEstate.Api;

namespace RealEstate.Tests.Integration;

public class AdminAnalyticsEndpointsTests
{
    private WebApplicationFactory<RealEstate.Api.Program> _factory = default!;
    private HttpClient _client = default!;

    [SetUp]
    public void Setup()
    {
        _factory = new IntegrationTestWebAppFactory();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task GetDashboardAnalytics_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/analytics/dashboard");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }

    [Test]
    public async Task GetDashboardAnalytics_WithDateRange_ShouldReturnOk()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-6);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync(
            $"/api/admin/analytics/dashboard?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }

    [Test]
    public async Task GetPropertyAnalytics_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/analytics/properties");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }

    [Test]
    public async Task GetPropertyAnalytics_WithOperationType_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/analytics/properties?operationType=sale");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }

    [Test]
    public async Task GetOwnerAnalytics_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/analytics/owners");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }

    [Test]
    public async Task GetRevenueAnalytics_ShouldReturnOk()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-12);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync(
            $"/api/admin/analytics/revenue?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }

    [Test]
    public async Task GetRevenueAnalytics_WithGroupBy_ShouldReturnOk()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-6);
        var endDate = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync(
            $"/api/admin/analytics/revenue?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&groupBy=day");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }

    [Test]
    public async Task GetPerformanceMetrics_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/analytics/performance");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<object>(content);
        Assert.That(analytics, Is.Not.Null);
    }
}
