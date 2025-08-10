using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace RealEstate.Application;

public interface IAuthService
{
    Task<(string token, AdminUser user)?> LoginAsync(string email, string password, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    private readonly IAdminUserRepository _repo;
    private readonly IConfiguration _config;

    public AuthService(IAdminUserRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    public async Task<(string token, AdminUser user)?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var normalized = email.Contains('@') ? email : $"{email}@millionluxury.com";
        var user = await _repo.GetByEmailAsync(normalized, ct);
        if (user is null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        var jwtKey = _config["JWT:KEY"] ?? throw new InvalidOperationException("JWT:KEY missing");
        var issuer = _config["JWT:ISSUER"] ?? "millionluxury";
        var audience = _config["JWT:AUDIENCE"] ?? "millionluxury-admin";
        var expiresMin = int.TryParse(_config["JWT:EXPIRES_MIN"], out var m) ? m : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMin),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, user);
    }
}


