using System.Security.Claims;
using AgOne.Shared.Auth.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Providers;

/// <summary>
/// Custom authentication state provider for AG ONE SSO.
/// Extends the default remote auth state provider to add:
/// - Cross-product SSO session validation
/// - Custom claims mapping from Entra ID tokens
/// - Automatic silent token renewal
/// - Redirect to AG ONE Portal when unauthenticated
/// </summary>
public class AgOneAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly AgOneSsoSettings _settings;
    private readonly ILogger<AgOneAuthStateProvider> _logger;
    private readonly AuthenticationStateProvider _underlying;

    private ClaimsPrincipal _cachedUser = new(new ClaimsIdentity());
    private DateTimeOffset _lastCheck = DateTimeOffset.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public AgOneAuthStateProvider(
        IAccessTokenProvider tokenProvider,
        IOptions<AgOneSsoSettings> settings,
        ILogger<AgOneAuthStateProvider> logger,
        RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, MsalProviderOptions> remoteAuthService)
        : base()
    {
        _tokenProvider = tokenProvider;
        _settings = settings.Value;
        _logger = logger;
        _underlying = remoteAuthService;

        // Listen for auth state changes from the underlying provider
        _underlying.AuthenticationStateChanged += OnUnderlyingAuthStateChanged;
    }

    private void OnUnderlyingAuthStateChanged(Task<AuthenticationState> task)
    {
        // Invalidate cache and propagate the change
        _lastCheck = DateTimeOffset.MinValue;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Check cache first
        if (DateTimeOffset.UtcNow - _lastCheck < _cacheExpiration
            && _cachedUser.Identity?.IsAuthenticated == true)
        {
            return new AuthenticationState(_cachedUser);
        }

        try
        {
            // Get the authentication state from the underlying MSAL provider
            var state = await _underlying.GetAuthenticationStateAsync();
            var user = state.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                // Try to get an access token to validate the session is still active
                var tokenResult = await _tokenProvider.RequestAccessToken(
                    new AccessTokenRequestOptions
                    {
                        Scopes = _settings.DefaultScopes.ToArray()
                    });

                if (tokenResult.TryGetToken(out var token))
                {
                    // Token is valid, create enriched claims principal
                    var enrichedUser = EnrichClaimsPrincipal(user, token);
                    _cachedUser = enrichedUser;
                    _lastCheck = DateTimeOffset.UtcNow;

                    _logger.LogDebug(
                        "AG ONE SSO: User {UserId} authenticated for product {Product}",
                        enrichedUser.FindFirst("oid")?.Value ?? "unknown",
                        _settings.ProductName);

                    return new AuthenticationState(enrichedUser);
                }
                else
                {
                    _logger.LogWarning(
                        "AG ONE SSO: Token acquisition failed for product {Product}. " +
                        "Token status: {Status}",
                        _settings.ProductName,
                        tokenResult.Status);

                    // Token expired or unavailable, return unauthenticated
                    return CreateUnauthenticatedState();
                }
            }

            return CreateUnauthenticatedState();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            _logger.LogWarning(ex,
                "AG ONE SSO: Access token not available for product {Product}",
                _settings.ProductName);

            return CreateUnauthenticatedState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AG ONE SSO: Error getting authentication state for product {Product}",
                _settings.ProductName);

            return CreateUnauthenticatedState();
        }
    }

    /// <summary>
    /// Enriches the claims principal with additional AG ONE-specific claims.
    /// Maps Entra ID token claims to application-friendly claim names.
    /// </summary>
    private ClaimsPrincipal EnrichClaimsPrincipal(ClaimsPrincipal original, AccessToken token)
    {
        var claims = new List<Claim>(original.Claims);

        // Add AG ONE product context claim
        if (!claims.Any(c => c.Type == "agone_product"))
        {
            claims.Add(new Claim("agone_product", _settings.ProductName));
        }

        // Add token expiration claim for UI components
        if (token.Expires != default)
        {
            claims.Add(new Claim("token_expires",
                token.Expires.ToString("o"),
                ClaimValueTypes.DateTime));
        }

        // Map common Entra ID claims to friendly names if they exist
        MapClaim(claims, "preferred_username", "email");
        MapClaim(claims, "name", "display_name");
        MapClaim(claims, "oid", "user_id");

        var identity = new ClaimsIdentity(claims, "AgOneSso", "name", "role");
        return new ClaimsPrincipal(identity);
    }

    private static void MapClaim(List<Claim> claims, string sourceType, string targetType)
    {
        if (!claims.Any(c => c.Type == targetType))
        {
            var sourceClaim = claims.FirstOrDefault(c => c.Type == sourceType);
            if (sourceClaim != null)
            {
                claims.Add(new Claim(targetType, sourceClaim.Value));
            }
        }
    }

    private AuthenticationState CreateUnauthenticatedState()
    {
        _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
        _lastCheck = DateTimeOffset.MinValue;
        return new AuthenticationState(_cachedUser);
    }

    /// <summary>
    /// Forces a refresh of the authentication state.
    /// Call this after cross-product navigation or when you suspect
    /// the token may have been refreshed.
    /// </summary>
    public void InvalidateCache()
    {
        _lastCheck = DateTimeOffset.MinValue;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
