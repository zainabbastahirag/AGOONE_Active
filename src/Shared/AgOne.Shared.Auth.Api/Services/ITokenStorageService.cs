using AgOne.Shared.Auth.Api.Data.Entities;

namespace AgOne.Shared.Auth.Api.Services;

/// <summary>
/// Service interface for storing, retrieving, and refreshing user tokens in the database.
/// Implement this in your API backend to persist SSO tokens.
/// </summary>
public interface ITokenStorageService
{
    // =========================================================================
    // TOKEN CRUD OPERATIONS
    // =========================================================================

    /// <summary>
    /// Saves or updates a user's token in the database.
    /// If a token already exists for this user+product, it is updated (upsert).
    /// </summary>
    Task<UserToken> SaveTokenAsync(SaveTokenRequest request);

    /// <summary>
    /// Retrieves the stored token for a user and product.
    /// Returns null if no token exists or if the token is inactive.
    /// </summary>
    Task<UserToken?> GetTokenAsync(string userId, string productName);

    /// <summary>
    /// Retrieves all active tokens for a user across all products.
    /// Useful for the Portal to see which products the user is active in.
    /// </summary>
    Task<IReadOnlyList<UserToken>> GetAllUserTokensAsync(string userId);

    /// <summary>
    /// Deactivates (soft-deletes) a user's token for a specific product.
    /// Called on logout from a single product.
    /// </summary>
    Task DeactivateTokenAsync(string userId, string productName);

    /// <summary>
    /// Deactivates all tokens for a user across all products.
    /// Called on global logout from AG ONE Portal.
    /// </summary>
    Task DeactivateAllUserTokensAsync(string userId);

    /// <summary>
    /// Checks if a user has a valid (active + not expired) token for a product.
    /// </summary>
    Task<bool> HasValidTokenAsync(string userId, string productName);

    // =========================================================================
    // REFRESH TOKEN OPERATIONS
    // =========================================================================

    /// <summary>
    /// Uses the stored refresh token to obtain a new access token from Entra ID.
    /// Updates the database with the new tokens.
    /// Returns the new access token, or null if refresh failed.
    /// </summary>
    Task<TokenRefreshResult> RefreshTokenAsync(string userId, string productName);

    /// <summary>
    /// Gets a valid access token for a user+product.
    /// If the current token is expired, automatically attempts a refresh.
    /// This is the main method you should call from your services.
    /// </summary>
    Task<string?> GetValidAccessTokenAsync(string userId, string productName);

    // =========================================================================
    // SESSION OPERATIONS
    // =========================================================================

    /// <summary>
    /// Creates or updates a user session record.
    /// </summary>
    Task<UserSession> TrackSessionAsync(TrackSessionRequest request);

    /// <summary>
    /// Gets all active sessions for a user.
    /// </summary>
    Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(string userId);

    /// <summary>
    /// Ends a specific session (on logout).
    /// </summary>
    Task EndSessionAsync(string sessionId);

    /// <summary>
    /// Ends all sessions for a user (on global logout).
    /// </summary>
    Task EndAllSessionsAsync(string userId);

    // =========================================================================
    // CLEANUP OPERATIONS
    // =========================================================================

    /// <summary>
    /// Removes expired and inactive token records older than the specified age.
    /// Call this from a background job (e.g., daily).
    /// </summary>
    Task<int> CleanupExpiredTokensAsync(TimeSpan olderThan);

    /// <summary>
    /// Removes inactive session records older than the specified age.
    /// </summary>
    Task<int> CleanupExpiredSessionsAsync(TimeSpan olderThan);
}

/// <summary>
/// Request model for saving a token.
/// </summary>
public class SaveTokenRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public DateTime AccessTokenExpiresUtc { get; set; }
    public DateTime? RefreshTokenExpiresUtc { get; set; }
    public string? GrantedScopes { get; set; }
    public string? TenantId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Result of a token refresh operation.
/// </summary>
public class TokenRefreshResult
{
    public bool Success { get; set; }
    public string? NewAccessToken { get; set; }
    public string? NewRefreshToken { get; set; }
    public DateTime? ExpiresUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public static TokenRefreshResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };

    public static TokenRefreshResult Succeeded(string accessToken, string? refreshToken, DateTime expiresUtc) =>
        new()
        {
            Success = true,
            NewAccessToken = accessToken,
            NewRefreshToken = refreshToken,
            ExpiresUtc = expiresUtc
        };
}

/// <summary>
/// Request model for tracking a session.
/// </summary>
public class TrackSessionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
