using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgOne.Shared.Auth.Api.Services;

/// <summary>
/// Background service that periodically cleans up expired tokens and sessions.
/// Runs once every 6 hours by default.
/// 
/// Register in Program.cs:
///   builder.Services.AddHostedService<TokenCleanupBackgroundService>();
/// </summary>
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;

    // Run cleanup every 6 hours
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    // Remove inactive tokens older than 30 days
    private static readonly TimeSpan TokenRetention = TimeSpan.FromDays(30);

    // Remove inactive sessions older than 90 days
    private static readonly TimeSpan SessionRetention = TimeSpan.FromDays(90);

    public TokenCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AG ONE TokenCleanup: Background service started. " +
            "Interval: {Interval}, Token retention: {TokenRetention}, " +
            "Session retention: {SessionRetention}",
            CleanupInterval, TokenRetention, SessionRetention);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var tokenStorage = scope.ServiceProvider
                    .GetRequiredService<ITokenStorageService>();

                var tokensRemoved = await tokenStorage
                    .CleanupExpiredTokensAsync(TokenRetention);
                var sessionsRemoved = await tokenStorage
                    .CleanupExpiredSessionsAsync(SessionRetention);

                _logger.LogInformation(
                    "AG ONE TokenCleanup: Removed {Tokens} tokens, {Sessions} sessions",
                    tokensRemoved, sessionsRemoved);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down - expected
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AG ONE TokenCleanup: Error during cleanup");
            }
        }
    }
}
