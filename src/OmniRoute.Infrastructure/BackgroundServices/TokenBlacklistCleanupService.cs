using OmniRoute.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OmniRoute.Infrastructure.BackgroundServices;

public class TokenBlacklistCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenBlacklistCleanupService> _logger;

    public TokenBlacklistCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<TokenBlacklistCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredTokensAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var now = DateTime.UtcNow;
            var expired = await context.TokenBlacklist
                .Where(t => t.ExpiresAt <= now)
                .ToListAsync(ct);

            if (expired.Count > 0)
            {
                context.TokenBlacklist.RemoveRange(expired);
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens.", expired.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred during token blacklist cleanup.");
        }
    }
}
