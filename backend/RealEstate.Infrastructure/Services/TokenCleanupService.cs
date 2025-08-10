using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealEstate.Application;

namespace RealEstate.Infrastructure.Services;

public sealed class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour

    public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupTokensAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            var blacklistService = scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>();

            // Cleanup expired refresh tokens
            // TODO: Implement refresh token cleanup on repository if required
            
            // Cleanup expired blacklisted tokens
            await blacklistService.CleanupExpiredBlacklistedTokensAsync(ct);

            _logger.LogInformation("Token cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token cleanup");
        }
    }
}
