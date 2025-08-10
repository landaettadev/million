using MongoDB.Driver;
using RealEstate.Application;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public sealed class TokenBlacklistService : ITokenBlacklistService
{
    private readonly MongoContext _context;

    public TokenBlacklistService(MongoContext context)
    {
        _context = context;
    }

    public async Task AddToBlacklistAsync(string token, string userId, string reason, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        try
        {
            // Extract expiration from JWT token
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (tokenHandler.CanReadToken(token))
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var expiresAt = jwtToken.ValidTo;

                var blacklistedToken = new TokenBlacklistDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    Token = token,
                    UserId = userId,
                    ExpiresAt = expiresAt,
                    BlacklistedAt = DateTime.UtcNow,
                    Reason = reason,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _context.TokenBlacklist.InsertOneAsync(blacklistedToken, cancellationToken: ct);
            }
        }
        catch
        {
            // If we can't parse the token, still add it to blacklist with a default expiration
            var blacklistedToken = new TokenBlacklistDocument
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Token = token,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(1), // Default 1 day
                BlacklistedAt = DateTime.UtcNow,
                Reason = reason,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _context.TokenBlacklist.InsertOneAsync(blacklistedToken, cancellationToken: ct);
        }
    }

    public async Task<bool> IsBlacklistedAsync(string token, CancellationToken ct = default)
    {
        var filter = Builders<TokenBlacklistDocument>.Filter.Eq(x => x.Token, token);
        var blacklistedToken = await _context.TokenBlacklist.Find(filter).FirstOrDefaultAsync(ct);
        
        if (blacklistedToken is null)
            return false;

        // Clean up expired blacklisted tokens
        if (blacklistedToken.ExpiresAt < DateTime.UtcNow)
        {
            await _context.TokenBlacklist.DeleteOneAsync(filter, ct);
            return false;
        }

        return true;
    }

    public async Task CleanupExpiredBlacklistedTokensAsync(CancellationToken ct = default)
    {
        var filter = Builders<TokenBlacklistDocument>.Filter.Lt(x => x.ExpiresAt, DateTime.UtcNow);
        await _context.TokenBlacklist.DeleteManyAsync(filter, ct);
    }
}
