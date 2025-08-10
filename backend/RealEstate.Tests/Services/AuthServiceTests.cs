using Microsoft.Extensions.Configuration;
using NSubstitute;
using RealEstate.Application;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using RealEstate.Application;

namespace RealEstate.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private IAdminUserRepository _mockRepo;
    private IConfiguration _mockConfig;
    private AuthService _authService;
    private AdminUser _testUser;

    [SetUp]
    public void Setup()
    {
        _mockRepo = Substitute.For<IAdminUserRepository>();
        _mockConfig = Substitute.For<IConfiguration>();
        _testUser = new AdminUser("123", "admin@millionluxury.com", "Admin User", "Admin", BCrypt.Net.BCrypt.HashPassword("admin123"));

        // Setup configuration
        _mockConfig["JWT:KEY"].Returns("test-secret-key-that-is-long-enough-for-hmac-sha256");
        _mockConfig["JWT:ISSUER"].Returns("test-issuer");
        _mockConfig["JWT:AUDIENCE"].Returns("test-audience");
        _mockConfig["JWT:EXPIRES_MIN"].Returns("60");

        _authService = new AuthService(_mockRepo, _mockConfig);
    }

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        // Arrange
        var email = "admin@millionluxury.com";
        var password = "admin123";
        _mockRepo.GetByEmailAsync(email, Arg.Any<CancellationToken>())
                 .Returns(_testUser);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.That(result, Is.Not.Null);
        var (token, user) = result.Value;
        Assert.That(token, Is.Not.Null);
        Assert.That(user.Email, Is.EqualTo(email));
        Assert.That(user.Role, Is.EqualTo("Admin"));
        
        // Verify token is valid JWT
        Assert.That(token, Does.Match(@"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]*$"));
    }

    [Test]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var email = "admin@millionluxury.com";
        var password = "wrongpassword";
        _mockRepo.GetByEmailAsync(email, Arg.Any<CancellationToken>())
                 .Returns(_testUser);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@millionluxury.com";
        var password = "admin123";
        _mockRepo.GetByEmailAsync(email, Arg.Any<CancellationToken>())
                 .Returns((AdminUser?)null);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithEmailWithoutDomain_AddsDomain()
    {
        // Arrange
        var email = "admin";
        var password = "admin123";
        var expectedEmail = "admin@millionluxury.com";
        _mockRepo.GetByEmailAsync(expectedEmail, Arg.Any<CancellationToken>())
                 .Returns(_testUser);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.That(result, Is.Not.Null);
        await _mockRepo.Received(1).GetByEmailAsync(expectedEmail, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LoginAsync_WithMissingJwtKey_ThrowsException()
    {
        // Arrange
        var email = "admin@millionluxury.com";
        var password = "admin123";
        _mockConfig["JWT:KEY"].Returns((string?)null);
        _mockRepo.GetByEmailAsync(email, Arg.Any<CancellationToken>())
                 .Returns(_testUser);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _authService.LoginAsync(email, password));
        Assert.That(ex.Message, Is.EqualTo("JWT:KEY missing"));
    }

    // Removed RefreshToken and Logout tests – not part of current AuthService contract

    private string GenerateValidJwtToken(AdminUser user)
    {
        var jwtKey = "test-secret-key-that-is-long-enough-for-hmac-sha256";
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
            new(System.Security.Claims.ClaimTypes.Name, user.Name),
            new(System.Security.Claims.ClaimTypes.Role, user.Role)
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
