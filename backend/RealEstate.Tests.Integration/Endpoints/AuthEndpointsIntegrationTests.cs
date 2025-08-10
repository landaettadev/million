using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RealEstate.Application;
using RealEstate.Api;
using RealEstate.Tests.Integration.Infrastructure;

namespace RealEstate.Tests.Integration.Endpoints;

[TestFixture]
public class AuthEndpointsIntegrationTests
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
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        // Arrange
        var loginRequest = new { email = "admin@millionluxury.com", password = "admin123" };
        var expectedUser = new AdminUser("123", "admin@millionluxury.com", "Admin User", "Admin", "hash");
        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        _mockAuthService.Setup(x => x.LoginAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedToken, expectedUser));

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.token.ToString(), Is.EqualTo(expectedToken));
        Assert.That(result!.user.email.ToString(), Is.EqualTo(expectedUser.Email));
        Assert.That(result!.user.role.ToString(), Is.EqualTo(expectedUser.Role));
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new { email = "admin@millionluxury.com", password = "wrongpassword" };

        _mockAuthService.Setup(x => x.LoginAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((string, AdminUser)?));

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new { password = "admin123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new { email = "admin@millionluxury.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Refresh_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var refreshRequest = new { refreshToken = "valid.refresh.token" };
        var expectedUser = new AdminUser("123", "admin@millionluxury.com", "Admin User", "Admin", "hash");
        var expectedToken = "new.jwt.token";

        // Adjusted: RefreshTokenAsync not in current contract; skip mocking

        // Create an authenticated client
        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Add("Authorization", "Bearer valid.jwt.token");

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/admin/auth/refresh", refreshRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.token.ToString(), Is.EqualTo(expectedToken));
    }

    [Test]
    public async Task Refresh_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new { refreshToken = "valid.refresh.token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/auth/refresh", refreshRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new { refreshToken = "valid.refresh.token" };

        // Adjusted: RefreshTokenAsync not in current contract; skip mocking

        // Create an authenticated client
        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Add("Authorization", "Bearer valid.jwt.token");

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/admin/auth/refresh", refreshRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        // Adjusted: Logout not in current contract; skip mocking

        // Create an authenticated client
        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Add("Authorization", "Bearer valid.jwt.token");

        // Act
        var response = await authenticatedClient.PostAsync("/api/admin/auth/logout", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.message.ToString(), Is.EqualTo("Logged out successfully"));
    }

    [Test]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/admin/auth/logout", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AdminEndpoints_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Test that admin endpoints require authentication
        var endpoints = new[]
        {
            "/api/admin/properties",
            "/api/admin/owners",
            "/api/admin/analytics"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), 
                $"Endpoint {endpoint} should require authentication");
        }
    }

    [Test]
    public async Task AdminEndpoints_WithInvalidToken_ReturnsUnauthorized()
    {
        // Create a client with invalid token
        var clientWithInvalidToken = _factory.CreateClient();
        clientWithInvalidToken.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.token");

        var endpoints = new[]
        {
            "/api/admin/properties",
            "/api/admin/owners",
            "/api/admin/analytics"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await clientWithInvalidToken.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), 
                $"Endpoint {endpoint} should reject invalid tokens");
        }
    }
}
