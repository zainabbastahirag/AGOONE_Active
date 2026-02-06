namespace AgOne.Shared.Auth;

/// <summary>
/// Token storage interface. Implemented in Infrastructure project.
/// </summary>
public interface ITokenStorageService
{
    Task<UserTokenDto?> GetTokenAsync(string userId, string productName);
    Task<IReadOnlyList<UserTokenDto>> GetAllUserTokensAsync(string userId);
    Task<UserTokenDto> SaveTokenAsync(SaveTokenRequest request);
    Task DeactivateTokenAsync(string userId, string productName);
    Task DeactivateAllUserTokensAsync(string userId);
    Task<bool> HasValidTokenAsync(string userId, string productName);
    Task<TokenRefreshResult> RefreshTokenAsync(string userId, string productName);
    Task<string?> GetValidAccessTokenAsync(string userId, string productName);
    Task<UserSessionDto> TrackSessionAsync(TrackSessionRequest request);
    Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsAsync(string userId);
    Task EndSessionAsync(string sessionId);
    Task EndAllSessionsAsync(string userId);
    Task<int> CleanupExpiredTokensAsync(TimeSpan olderThan);
    Task<int> CleanupExpiredSessionsAsync(TimeSpan olderThan);
}

// ── DTOs ──

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
        new() { Success = true, NewAccessToken = accessToken, NewRefreshToken = refreshToken, ExpiresUtc = expiresUtc };
}

public class TrackSessionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class UserTokenDto
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime AccessTokenExpiresUtc { get; set; }
    public string? GrantedScopes { get; set; }
    public bool IsActive { get; set; }
    public int RefreshCount { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class UserSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string InitiatedByProduct { get; set; } = string.Empty;
    public string? ProductsAccessed { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
}
