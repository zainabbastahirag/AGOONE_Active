using System.Net.Http.Json;
using AgOne.Shared.Auth.Configuration;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Services;

/// <summary>
/// Client-side service that syncs tokens to the API backend for DB storage.
/// 
/// In Blazor WASM, tokens live in the browser (managed by MSAL.js).
/// This service sends them to your API so they can be stored in the database.
/// 
/// This is OPTIONAL - only needed if you want to store tokens server-side.
/// Common reasons:
/// - Background jobs that need to call APIs on behalf of users
/// - Server-to-server calls using user context
/// - Audit logging of all token grants
/// - Token revocation across all products from the server side
/// </summary>
public interface ITokenSyncService
{
    /// <summary>
    /// Sends the current access token to the API backend for database storage.
    /// Call this after successful authentication.
    /// </summary>
    Task SyncTokenToBackendAsync();

    /// <summary>
    /// Notifies the backend that the user has logged out.
    /// The backend will deactivate the stored tokens.
    /// </summary>
    Task NotifyLogoutAsync();
}

public class TokenSyncService : ITokenSyncService
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgOneSsoSettings _settings;
    private readonly ILogger<TokenSyncService> _logger;

    public TokenSyncService(
        IAccessTokenProvider tokenProvider,
        IHttpClientFactory httpClientFactory,
        IOptions<AgOneSsoSettings> settings,
        ILogger<TokenSyncService> logger)
    {
        _tokenProvider = tokenProvider;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SyncTokenToBackendAsync()
    {
        try
        {
            var tokenResult = await _tokenProvider.RequestAccessToken(
                new AccessTokenRequestOptions
                {
                    Scopes = _settings.DefaultScopes.ToArray()
                });

            if (!tokenResult.TryGetToken(out var token))
            {
                _logger.LogDebug("AG ONE TokenSync: No token available to sync");
                return;
            }

            var client = _httpClientFactory.CreateClient("AgOne.ProductApi");

            // Send the token to the backend's sync endpoint
            var syncRequest = new TokenSyncRequest
            {
                AccessToken = token.Value,
                ExpiresAt = token.Expires,
                GrantedScopes = string.Join(" ", token.GrantedScopes),
                ProductName = _settings.ProductName
            };

            var response = await client.PostAsJsonAsync(
                "api/agone-auth/sync-token", syncRequest);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "AG ONE TokenSync: Token synced to backend for product {Product}",
                    _settings.ProductName);
            }
            else
            {
                _logger.LogWarning(
                    "AG ONE TokenSync: Backend returned {Status} when syncing token",
                    response.StatusCode);
            }
        }
        catch (AccessTokenNotAvailableException)
        {
            _logger.LogDebug("AG ONE TokenSync: Token not available for sync");
        }
        catch (Exception ex)
        {
            // Token sync failure should not break the app
            _logger.LogWarning(ex, "AG ONE TokenSync: Failed to sync token to backend");
        }
    }

    public async Task NotifyLogoutAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AgOne.ProductApi");
            await client.PostAsync("api/agone-auth/logout", null);
            _logger.LogDebug("AG ONE TokenSync: Logout notified to backend");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AG ONE TokenSync: Failed to notify backend of logout");
        }
    }
}

/// <summary>
/// Request model for syncing tokens from client to server.
/// </summary>
public class TokenSyncRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? GrantedScopes { get; set; }
    public string ProductName { get; set; } = string.Empty;
}
