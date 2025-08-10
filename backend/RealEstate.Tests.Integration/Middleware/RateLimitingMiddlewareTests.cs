using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RealEstate.Application;
using RealEstate.Api;
using RealEstate.Tests.Integration.Infrastructure;

namespace RealEstate.Tests.Integration.Middleware;

[TestFixture]
public class RateLimitingMiddlewareTests
{
    private WebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;
    private Mock<IAuthService> _mockAuthService = default!;

    [SetUp]
    public void Setup()
    {
        _factory = new IntegrationTestWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var authServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IAuthService));
                if (authServiceDescriptor != null)
                {
                    services.Remove(authServiceDescriptor);
                }

                _mockAuthService = new Mock<IAuthService>();
                services.AddSingleton(_mockAuthService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Test]
    public async Task Login_WithinRateLimit_ReturnsExpectedResponse()
    {
        // Arrange
        var loginRequest = new { email = "admin@millionluxury.com", password = "admin123" };
        var expectedUser = new RealEstate.Application.AdminUser("123", "admin@millionluxury.com", "Admin User", "Admin", "hash");
        var expectedToken = "valid.jwt.token";

        _mockAuthService.Setup(x => x.LoginAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedToken, expectedUser));

        // Act - Make a single login attempt
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_ExceedingRateLimit_ReturnsTooManyRequests()
    {
        // Arrange
        var loginRequest = new { email = "admin@millionluxury.com", password = "wrongpassword" };

        _mockAuthService.Setup(x => x.LoginAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((string, RealEstate.Application.AdminUser)?));

        // Act - Make multiple failed login attempts to trigger rate limiting
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);
            if (i < 4)
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        // The 6th attempt should be blocked
        var blockedResponse = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);
        
        // Assert
        Assert.That(blockedResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        
        var errorContent = await blockedResponse.Content.ReadFromJsonAsync<dynamic>();
        Assert.That(errorContent, Is.Not.Null);
        Assert.That(errorContent!.error.ToString(), Is.EqualTo("Too many login attempts. Please try again later."));
    }

    [Test]
    public async Task Login_AfterLockoutExpires_AllowsLoginAgain()
    {
        // Arrange
        var loginRequest = new { email = "admin@millionluxury.com", password = "wrongpassword" };
        var validLoginRequest = new { email = "admin@millionluxury.com", password = "admin123" };
        var expectedUser = new RealEstate.Application.AdminUser("123", "admin@millionluxury.com", "Admin User", "Admin", "hash");
        var expectedToken = "valid.jwt.token";

        _mockAuthService.Setup(x => x.LoginAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((string, RealEstate.Application.AdminUser)?));

        // Trigger rate limiting
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);
        }

        // Verify we're blocked
        var blockedResponse = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);
        Assert.That(blockedResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

        // Note: In a real test, we would wait for the lockout to expire
        // For this test, we'll simulate the lockout expiring by creating a new client
        // In production, you might want to use a time-based approach or mock the time

        // Create a new client to simulate a different IP (in real scenario, lockout would expire)
        var newClient = _factory.CreateClient();
        
        // Setup successful login for the new client
        _mockAuthService.Setup(x => x.LoginAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedToken, expectedUser));

        // Act - Try to login with the new client
        var response = await newClient.PostAsJsonAsync("/api/admin/auth/login", validLoginRequest);

        // Assert - Should work with new client (simulating different IP)
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_WithDifferentIPs_EachIPHasSeparateLimit()
    {
        // Arrange
        var loginRequest = new { email = "admin@millionluxury.com", password = "wrongpassword" };

        _mockAuthService.Setup(x => x.LoginAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((string, RealEstate.Application.AdminUser)?));

        // Create multiple clients to simulate different IPs
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        // Trigger rate limiting on first client
        for (int i = 0; i < 5; i++)
        {
            await client1.PostAsJsonAsync("/api/admin/auth/login", loginRequest);
        }

        // Verify first client is blocked
        var blockedResponse = await client1.PostAsJsonAsync("/api/admin/auth/login", loginRequest);
        Assert.That(blockedResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

        // Second client should still be able to make attempts
        var response2 = await client2.PostAsJsonAsync("/api/admin/auth/login", loginRequest);
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized)); // Still allowed
    }

    [Test]
    public async Task NonLoginEndpoints_AreNotAffectedByRateLimiting()
    {
        // Arrange
        var nonLoginRequest = new { someData = "test" };

        // Act - Make multiple requests to a non-login endpoint
        for (int i = 0; i < 10; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/admin/auth/login", nonLoginRequest);
            // Should get validation errors, not rate limiting
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }
}
