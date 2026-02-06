using System.Text.Json;
using System.Text.Json.Serialization;
using AgOne.Infrastructure.Auth.Entities;
using AgOne.Shared.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Infrastructure.Auth;

public class TokenStorageService : ITokenStorageService
{
    private readonly DbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgOneApiAuthSettings _settings;
    private readonly ILogger<TokenStorageService> _logger;

    /// <summary>
    /// Inject YOUR existing DbContext here. See STEP-BY-STEP.md Step 3.
    /// </summary>
    public TokenStorageService(
        DbContext db,  // ← Replace with your actual DbContext type
        IHttpClientFactory httpClientFactory,
        IOptions<AgOneApiAuthSettings> settings,
        ILogger<TokenStorageService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<UserTokenDto> SaveTokenAsync(SaveTokenRequest request)
    {
        var existing = await _db.Set<UserToken>()
            .FirstOrDefaultAsync(t => t.UserId == request.UserId && t.ProductName == request.ProductName);

        if (existing != null)
        {
            existing.AccessToken = request.AccessToken;
            existing.RefreshToken = request.RefreshToken ?? existing.RefreshToken;
            existing.IdToken = request.IdToken ?? existing.IdToken;
            existing.AccessTokenExpiresUtc = request.AccessTokenExpiresUtc;
            existing.RefreshTokenExpiresUtc = request.RefreshTokenExpiresUtc ?? existing.RefreshTokenExpiresUtc;
            existing.GrantedScopes = request.GrantedScopes ?? existing.GrantedScopes;
            existing.TenantId = request.TenantId ?? existing.TenantId;
            existing.Email = request.Email ?? existing.Email;
            existing.DisplayName = request.DisplayName ?? existing.DisplayName;
            existing.LastIpAddress = request.IpAddress;
            existing.LastUserAgent = request.UserAgent;
            existing.IsActive = true;
            existing.UpdatedUtc = DateTime.UtcNow;
            existing.RefreshCount++;
            await _db.SaveChangesAsync();
            return MapToDto(existing);
        }

        var token = new UserToken
        {
            UserId = request.UserId, Email = request.Email, DisplayName = request.DisplayName,
            ProductName = request.ProductName, AccessToken = request.AccessToken,
            RefreshToken = request.RefreshToken, IdToken = request.IdToken,
            AccessTokenExpiresUtc = request.AccessTokenExpiresUtc,
            RefreshTokenExpiresUtc = request.RefreshTokenExpiresUtc,
            GrantedScopes = request.GrantedScopes, TenantId = request.TenantId,
            LastIpAddress = request.IpAddress, LastUserAgent = request.UserAgent,
            IsActive = true, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow
        };
        _db.Set<UserToken>().Add(token);
        await _db.SaveChangesAsync();
        return MapToDto(token);
    }

    public async Task<UserTokenDto?> GetTokenAsync(string userId, string productName)
    {
        var t = await _db.Set<UserToken>().FirstOrDefaultAsync(
            x => x.UserId == userId && x.ProductName == productName && x.IsActive);
        return t == null ? null : MapToDto(t);
    }

    public async Task<IReadOnlyList<UserTokenDto>> GetAllUserTokensAsync(string userId) =>
        (await _db.Set<UserToken>().Where(t => t.UserId == userId && t.IsActive).ToListAsync())
            .Select(MapToDto).ToList();

    public async Task DeactivateTokenAsync(string userId, string productName)
    {
        var t = await _db.Set<UserToken>().FirstOrDefaultAsync(x => x.UserId == userId && x.ProductName == productName);
        if (t != null) { t.IsActive = false; t.UpdatedUtc = DateTime.UtcNow; await _db.SaveChangesAsync(); }
    }

    public async Task DeactivateAllUserTokensAsync(string userId)
    {
        var tokens = await _db.Set<UserToken>().Where(t => t.UserId == userId && t.IsActive).ToListAsync();
        tokens.ForEach(t => { t.IsActive = false; t.UpdatedUtc = DateTime.UtcNow; });
        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasValidTokenAsync(string userId, string productName) =>
        await _db.Set<UserToken>().AnyAsync(t =>
            t.UserId == userId && t.ProductName == productName && t.IsActive && t.AccessTokenExpiresUtc > DateTime.UtcNow);

    public async Task<TokenRefreshResult> RefreshTokenAsync(string userId, string productName)
    {
        var stored = await _db.Set<UserToken>().FirstOrDefaultAsync(
            t => t.UserId == userId && t.ProductName == productName && t.IsActive);
        if (stored == null) return TokenRefreshResult.Failed("No token found");
        if (string.IsNullOrEmpty(stored.RefreshToken)) return TokenRefreshResult.Failed("No refresh token available");
        if (stored.RefreshTokenExpiresUtc.HasValue && stored.RefreshTokenExpiresUtc.Value < DateTime.UtcNow)
            return TokenRefreshResult.Failed("Refresh token expired");

        try
        {
            var authority = _settings.Instance.TrimEnd('/');
            var endpoint = !string.IsNullOrEmpty(_settings.SignUpSignInPolicyId)
                ? $"{authority}/{_settings.TenantId}/{_settings.SignUpSignInPolicyId}/oauth2/v2.0/token"
                : $"{authority}/{_settings.TenantId}/oauth2/v2.0/token";

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _settings.ClientId,
                ["refresh_token"] = stored.RefreshToken!,
            };
            if (!string.IsNullOrEmpty(stored.GrantedScopes)) form["scope"] = stored.GrantedScopes;

            var client = _httpClientFactory.CreateClient("AgOne.EntraId");
            var resp = await client.PostAsync(endpoint, new FormUrlEncodedContent(form));
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return TokenRefreshResult.Failed($"Entra ID: {resp.StatusCode} {body}");

            var tr = JsonSerializer.Deserialize<EntraTokenResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (tr == null || string.IsNullOrEmpty(tr.AccessToken)) return TokenRefreshResult.Failed("Bad response");

            var expiresUtc = DateTime.UtcNow.AddSeconds(tr.ExpiresIn);
            stored.AccessToken = tr.AccessToken; stored.RefreshToken = tr.RefreshToken ?? stored.RefreshToken;
            stored.AccessTokenExpiresUtc = expiresUtc; stored.UpdatedUtc = DateTime.UtcNow; stored.RefreshCount++;
            await _db.SaveChangesAsync();
            return TokenRefreshResult.Succeeded(tr.AccessToken, tr.RefreshToken, expiresUtc);
        }
        catch (Exception ex) { return TokenRefreshResult.Failed(ex.Message); }
    }

    public async Task<string?> GetValidAccessTokenAsync(string userId, string productName)
    {
        var t = await _db.Set<UserToken>().FirstOrDefaultAsync(
            x => x.UserId == userId && x.ProductName == productName && x.IsActive);
        if (t == null) return null;
        if (t.AccessTokenExpiresUtc > DateTime.UtcNow.AddMinutes(5)) return t.AccessToken;
        var r = await RefreshTokenAsync(userId, productName);
        return r.Success ? r.NewAccessToken : null;
    }

    // ── Sessions ──

    public async Task<UserSessionDto> TrackSessionAsync(TrackSessionRequest request)
    {
        var existing = await _db.Set<UserSession>().Where(s => s.UserId == request.UserId && s.IsActive)
            .OrderByDescending(s => s.LastActivityUtc).FirstOrDefaultAsync();
        if (existing != null)
        {
            existing.LastActivityUtc = DateTime.UtcNow;
            var products = existing.ProductsAccessed?.Split(',').ToList() ?? new List<string>();
            if (!products.Contains(request.ProductName)) { products.Add(request.ProductName); existing.ProductsAccessed = string.Join(",", products); }
            await _db.SaveChangesAsync();
            return MapSessionDto(existing);
        }
        var s = new UserSession { SessionId = Guid.NewGuid().ToString(), UserId = request.UserId, Email = request.Email,
            InitiatedByProduct = request.ProductName, ProductsAccessed = request.ProductName,
            IpAddress = request.IpAddress, UserAgent = request.UserAgent };
        _db.Set<UserSession>().Add(s);
        await _db.SaveChangesAsync();
        return MapSessionDto(s);
    }

    public async Task<IReadOnlyList<UserSessionDto>> GetActiveSessionsAsync(string userId) =>
        (await _db.Set<UserSession>().Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityUtc).ToListAsync()).Select(MapSessionDto).ToList();

    public async Task EndSessionAsync(string sessionId)
    {
        var s = await _db.Set<UserSession>().FirstOrDefaultAsync(x => x.SessionId == sessionId);
        if (s != null) { s.IsActive = false; s.EndedUtc = DateTime.UtcNow; await _db.SaveChangesAsync(); }
    }

    public async Task EndAllSessionsAsync(string userId)
    {
        var sessions = await _db.Set<UserSession>().Where(s => s.UserId == userId && s.IsActive).ToListAsync();
        sessions.ForEach(s => { s.IsActive = false; s.EndedUtc = DateTime.UtcNow; });
        await _db.SaveChangesAsync();
    }

    public async Task<int> CleanupExpiredTokensAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow.Subtract(olderThan);
        var expired = await _db.Set<UserToken>().Where(t => !t.IsActive && t.UpdatedUtc < cutoff).ToListAsync();
        _db.Set<UserToken>().RemoveRange(expired);
        await _db.SaveChangesAsync();
        return expired.Count;
    }

    public async Task<int> CleanupExpiredSessionsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow.Subtract(olderThan);
        var expired = await _db.Set<UserSession>().Where(s => !s.IsActive && (s.EndedUtc ?? s.LastActivityUtc) < cutoff).ToListAsync();
        _db.Set<UserSession>().RemoveRange(expired);
        await _db.SaveChangesAsync();
        return expired.Count;
    }

    // ── Helpers ──

    private static UserTokenDto MapToDto(UserToken t) => new()
    {
        UserId = t.UserId, Email = t.Email, DisplayName = t.DisplayName,
        ProductName = t.ProductName, AccessToken = t.AccessToken, RefreshToken = t.RefreshToken,
        AccessTokenExpiresUtc = t.AccessTokenExpiresUtc, GrantedScopes = t.GrantedScopes,
        IsActive = t.IsActive, RefreshCount = t.RefreshCount, UpdatedUtc = t.UpdatedUtc
    };

    private static UserSessionDto MapSessionDto(UserSession s) => new()
    {
        SessionId = s.SessionId, UserId = s.UserId, Email = s.Email,
        InitiatedByProduct = s.InitiatedByProduct, ProductsAccessed = s.ProductsAccessed,
        StartedUtc = s.StartedUtc, LastActivityUtc = s.LastActivityUtc, IsActive = s.IsActive, IpAddress = s.IpAddress
    };
}

internal class EntraTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
}
