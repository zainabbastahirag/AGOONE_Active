using System.Net.Http.Headers;
using System.Text.Json;
using AgOne.Shared.Auth.Api.Configuration;
using AgOne.Shared.Auth.Api.Data;
using AgOne.Shared.Auth.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Api.Services;

/// <summary>
/// Implementation of token storage service using Entity Framework Core.
/// Stores tokens in the database and handles refresh token flow with Entra ID.
/// 
/// This service works with either:
/// - AgOneTokenDbContext (standalone)
/// - Your existing DbContext (if you added the UserToken/UserSession entities to it)
/// </summary>
public class TokenStorageService : ITokenStorageService
{
    private readonly AgOneTokenDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgOneApiAuthSettings _settings;
    private readonly ILogger<TokenStorageService> _logger;

    // Buffer before actual expiry to trigger refresh proactively
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(5);

    public TokenStorageService(
        AgOneTokenDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<AgOneApiAuthSettings> settings,
        ILogger<TokenStorageService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    // =========================================================================
    // TOKEN CRUD
    // =========================================================================

    public async Task<UserToken> SaveTokenAsync(SaveTokenRequest request)
    {
        var existing = await _db.UserTokens
            .FirstOrDefaultAsync(t =>
                t.UserId == request.UserId &&
                t.ProductName == request.ProductName);

        if (existing != null)
        {
            // Update existing token
            existing.AccessToken = request.AccessToken;
            existing.RefreshToken = request.RefreshToken;
            existing.IdToken = request.IdToken;
            existing.AccessTokenExpiresUtc = request.AccessTokenExpiresUtc;
            existing.RefreshTokenExpiresUtc = request.RefreshTokenExpiresUtc;
            existing.GrantedScopes = request.GrantedScopes;
            existing.TenantId = request.TenantId;
            existing.Email = request.Email ?? existing.Email;
            existing.DisplayName = request.DisplayName ?? existing.DisplayName;
            existing.LastIpAddress = request.IpAddress;
            existing.LastUserAgent = request.UserAgent;
            existing.IsActive = true;
            existing.UpdatedUtc = DateTime.UtcNow;
            existing.RefreshCount++;

            _db.UserTokens.Update(existing);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "AG ONE TokenStorage: Updated token for user {UserId} product {Product} " +
                "(refresh #{Count})",
                request.UserId, request.ProductName, existing.RefreshCount);

            return existing;
        }
        else
        {
            // Create new token record
            var token = new UserToken
            {
                UserId = request.UserId,
                Email = request.Email,
                DisplayName = request.DisplayName,
                ProductName = request.ProductName,
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken,
                IdToken = request.IdToken,
                AccessTokenExpiresUtc = request.AccessTokenExpiresUtc,
                RefreshTokenExpiresUtc = request.RefreshTokenExpiresUtc,
                GrantedScopes = request.GrantedScopes,
                TenantId = request.TenantId,
                LastIpAddress = request.IpAddress,
                LastUserAgent = request.UserAgent,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                RefreshCount = 0
            };

            _db.UserTokens.Add(token);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "AG ONE TokenStorage: Created token for user {UserId} product {Product}",
                request.UserId, request.ProductName);

            return token;
        }
    }

    public async Task<UserToken?> GetTokenAsync(string userId, string productName)
    {
        return await _db.UserTokens
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.ProductName == productName &&
                t.IsActive);
    }

    public async Task<IReadOnlyList<UserToken>> GetAllUserTokensAsync(string userId)
    {
        return await _db.UserTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .OrderBy(t => t.ProductName)
            .ToListAsync();
    }

    public async Task DeactivateTokenAsync(string userId, string productName)
    {
        var token = await _db.UserTokens
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.ProductName == productName);

        if (token != null)
        {
            token.IsActive = false;
            token.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "AG ONE TokenStorage: Deactivated token for user {UserId} product {Product}",
                userId, productName);
        }
    }

    public async Task DeactivateAllUserTokensAsync(string userId)
    {
        var tokens = await _db.UserTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsActive = false;
            token.UpdatedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "AG ONE TokenStorage: Deactivated {Count} tokens for user {UserId}",
            tokens.Count, userId);
    }

    public async Task<bool> HasValidTokenAsync(string userId, string productName)
    {
        return await _db.UserTokens.AnyAsync(t =>
            t.UserId == userId &&
            t.ProductName == productName &&
            t.IsActive &&
            t.AccessTokenExpiresUtc > DateTime.UtcNow);
    }

    // =========================================================================
    // REFRESH TOKEN FLOW
    // =========================================================================

    public async Task<TokenRefreshResult> RefreshTokenAsync(string userId, string productName)
    {
        var storedToken = await GetTokenAsync(userId, productName);

        if (storedToken == null)
        {
            return TokenRefreshResult.Failed("No stored token found for this user/product");
        }

        if (string.IsNullOrEmpty(storedToken.RefreshToken))
        {
            return TokenRefreshResult.Failed(
                "No refresh token available. In SPA (Blazor WASM) flows with Entra ID, " +
                "refresh tokens may not be issued. The client-side MSAL handles token " +
                "renewal via SSO session cookies instead.");
        }

        // Check if refresh token itself has expired
        if (storedToken.RefreshTokenExpiresUtc.HasValue &&
            storedToken.RefreshTokenExpiresUtc.Value < DateTime.UtcNow)
        {
            storedToken.IsActive = false;
            storedToken.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return TokenRefreshResult.Failed("Refresh token has expired. User must re-authenticate.");
        }

        try
        {
            // Call Entra ID token endpoint to exchange refresh token for new tokens
            var result = await ExchangeRefreshTokenAsync(storedToken);

            if (result.Success)
            {
                // Update the database with new tokens
                await SaveTokenAsync(new SaveTokenRequest
                {
                    UserId = userId,
                    ProductName = productName,
                    AccessToken = result.NewAccessToken!,
                    RefreshToken = result.NewRefreshToken ?? storedToken.RefreshToken,
                    IdToken = storedToken.IdToken,
                    AccessTokenExpiresUtc = result.ExpiresUtc!.Value,
                    RefreshTokenExpiresUtc = storedToken.RefreshTokenExpiresUtc,
                    GrantedScopes = storedToken.GrantedScopes,
                    TenantId = storedToken.TenantId,
                    Email = storedToken.Email,
                    DisplayName = storedToken.DisplayName,
                    IpAddress = storedToken.LastIpAddress,
                    UserAgent = storedToken.LastUserAgent
                });

                _logger.LogInformation(
                    "AG ONE TokenStorage: Successfully refreshed token for " +
                    "user {UserId} product {Product}",
                    userId, productName);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AG ONE TokenStorage: Failed to refresh token for " +
                "user {UserId} product {Product}",
                userId, productName);

            return TokenRefreshResult.Failed($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<string?> GetValidAccessTokenAsync(string userId, string productName)
    {
        var token = await GetTokenAsync(userId, productName);

        if (token == null)
        {
            _logger.LogDebug(
                "AG ONE TokenStorage: No token found for user {UserId} product {Product}",
                userId, productName);
            return null;
        }

        // Check if access token is still valid (with buffer)
        if (token.AccessTokenExpiresUtc > DateTime.UtcNow.Add(ExpiryBuffer))
        {
            return token.AccessToken;
        }

        // Access token expired or about to expire - try refresh
        _logger.LogDebug(
            "AG ONE TokenStorage: Access token expired/expiring for " +
            "user {UserId} product {Product}. Attempting refresh...",
            userId, productName);

        var refreshResult = await RefreshTokenAsync(userId, productName);

        if (refreshResult.Success)
        {
            return refreshResult.NewAccessToken;
        }

        _logger.LogWarning(
            "AG ONE TokenStorage: Token refresh failed for user {UserId} product {Product}: {Error}",
            userId, productName, refreshResult.ErrorMessage);

        return null;
    }

    /// <summary>
    /// Exchanges a refresh token for new access + refresh tokens via Entra ID token endpoint.
    /// 
    /// *** THIS IS THE CORE REFRESH TOKEN FLOW ***
    /// 
    /// POST https://{authority}/oauth2/v2.0/token
    /// Content-Type: application/x-www-form-urlencoded
    /// 
    /// grant_type=refresh_token
    /// &client_id={clientId}
    /// &refresh_token={refreshToken}
    /// &scope={scopes}
    /// </summary>
    private async Task<TokenRefreshResult> ExchangeRefreshTokenAsync(UserToken storedToken)
    {
        var httpClient = _httpClientFactory.CreateClient("AgOne.EntraIdTokenEndpoint");

        // Build the token endpoint URL
        var authority = _settings.Instance.TrimEnd('/');
        string tokenEndpoint;

        if (!string.IsNullOrEmpty(_settings.SignUpSignInPolicyId))
        {
            // B2C / External ID token endpoint
            tokenEndpoint = $"{authority}/{_settings.TenantId}" +
                $"/{_settings.SignUpSignInPolicyId}/oauth2/v2.0/token";
        }
        else
        {
            // Regular Entra ID token endpoint
            tokenEndpoint = $"{authority}/{_settings.TenantId}/oauth2/v2.0/token";
        }

        // Build the form data for the refresh token grant
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _settings.ClientId,
            ["refresh_token"] = storedToken.RefreshToken!,
        };

        // Add scopes if available
        if (!string.IsNullOrEmpty(storedToken.GrantedScopes))
        {
            formData["scope"] = storedToken.GrantedScopes;
        }

        // NOTE: For CONFIDENTIAL clients (server-side apps with a client secret),
        // you also need to include client_secret. For PUBLIC clients (SPAs like
        // Blazor WASM), client_secret is NOT used.
        // 
        // If your API is a confidential client, uncomment and configure:
        // formData["client_secret"] = _settings.ClientSecret;

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formData)
        };

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "AG ONE TokenStorage: Entra ID token refresh failed. " +
                "Status: {Status}, Response: {Response}",
                response.StatusCode, responseBody);

            return TokenRefreshResult.Failed(
                $"Entra ID returned {response.StatusCode}: {responseBody}");
        }

        // Parse the token response
        var tokenResponse = JsonSerializer.Deserialize<EntraIdTokenResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            return TokenRefreshResult.Failed("Failed to parse token response from Entra ID");
        }

        var expiresUtc = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        return TokenRefreshResult.Succeeded(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken, // May be a new refresh token (rotation)
            expiresUtc);
    }

    // =========================================================================
    // SESSION TRACKING
    // =========================================================================

    public async Task<UserSession> TrackSessionAsync(TrackSessionRequest request)
    {
        // Find an existing active session for this user
        var existingSession = await _db.UserSessions
            .Where(s => s.UserId == request.UserId && s.IsActive)
            .OrderByDescending(s => s.LastActivityUtc)
            .FirstOrDefaultAsync();

        if (existingSession != null)
        {
            // Update existing session
            existingSession.LastActivityUtc = DateTime.UtcNow;

            // Add the product to the accessed list if not already there
            var products = existingSession.ProductsAccessed?.Split(',').ToList()
                ?? new List<string>();
            if (!products.Contains(request.ProductName))
            {
                products.Add(request.ProductName);
                existingSession.ProductsAccessed = string.Join(",", products);
            }

            await _db.SaveChangesAsync();
            return existingSession;
        }

        // Create new session
        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = request.UserId,
            Email = request.Email,
            InitiatedByProduct = request.ProductName,
            ProductsAccessed = request.ProductName,
            StartedUtc = DateTime.UtcNow,
            LastActivityUtc = DateTime.UtcNow,
            IsActive = true,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "AG ONE TokenStorage: New session {SessionId} for user {UserId}",
            session.SessionId, session.UserId);

        return session;
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(string userId)
    {
        return await _db.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityUtc)
            .ToListAsync();
    }

    public async Task EndSessionAsync(string sessionId)
    {
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session != null)
        {
            session.IsActive = false;
            session.EndedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task EndAllSessionsAsync(string userId)
    {
        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "AG ONE TokenStorage: Ended {Count} sessions for user {UserId}",
            sessions.Count, userId);
    }

    // =========================================================================
    // CLEANUP
    // =========================================================================

    public async Task<int> CleanupExpiredTokensAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow.Subtract(olderThan);

        var expiredTokens = await _db.UserTokens
            .Where(t =>
                (!t.IsActive && t.UpdatedUtc < cutoff) ||
                (t.AccessTokenExpiresUtc < cutoff && !t.IsActive))
            .ToListAsync();

        _db.UserTokens.RemoveRange(expiredTokens);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "AG ONE TokenStorage: Cleaned up {Count} expired token records",
            expiredTokens.Count);

        return expiredTokens.Count;
    }

    public async Task<int> CleanupExpiredSessionsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow.Subtract(olderThan);

        var expiredSessions = await _db.UserSessions
            .Where(s => !s.IsActive && (s.EndedUtc ?? s.LastActivityUtc) < cutoff)
            .ToListAsync();

        _db.UserSessions.RemoveRange(expiredSessions);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "AG ONE TokenStorage: Cleaned up {Count} expired session records",
            expiredSessions.Count);

        return expiredSessions.Count;
    }
}

/// <summary>
/// Entra ID OAuth2 token endpoint response model.
/// </summary>
internal class EntraIdTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }

    // JSON property name mapping for snake_case
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessTokenJson { set => AccessToken = value; }

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshTokenJson { set => RefreshToken = value; }

    [System.Text.Json.Serialization.JsonPropertyName("id_token")]
    public string? IdTokenJson { set => IdToken = value; }

    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string TokenTypeJson { set => TokenType = value; }

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresInJson { set => ExpiresIn = value; }

    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    public string? ScopeJson { set => Scope = value; }
}
