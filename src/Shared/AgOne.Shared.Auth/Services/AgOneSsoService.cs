using System.Security.Claims;
using AgOne.Shared.Auth.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Services;

/// <summary>
/// Implementation of the AG ONE SSO service.
/// Handles cross-product authentication, navigation, and token management.
/// </summary>
public class AgOneSsoService : IAgOneSsoService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly NavigationManager _navigation;
    private readonly AgOneSsoSettings _settings;
    private readonly ILogger<AgOneSsoService> _logger;

    public AgOneSsoService(
        AuthenticationStateProvider authStateProvider,
        IAccessTokenProvider tokenProvider,
        NavigationManager navigation,
        IOptions<AgOneSsoSettings> settings,
        ILogger<AgOneSsoService> logger)
    {
        _authStateProvider = authStateProvider;
        _tokenProvider = tokenProvider;
        _navigation = navigation;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true;
    }

    public async Task<string?> GetUserDisplayNameAsync()
    {
        var user = await GetUserAsync();
        return user?.FindFirst("name")?.Value
            ?? user?.FindFirst("display_name")?.Value
            ?? user?.FindFirst("preferred_username")?.Value;
    }

    public async Task<string?> GetUserEmailAsync()
    {
        var user = await GetUserAsync();
        return user?.FindFirst("preferred_username")?.Value
            ?? user?.FindFirst("email")?.Value
            ?? user?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var user = await GetUserAsync();
        return user?.FindFirst("oid")?.Value
            ?? user?.FindFirst("sub")?.Value;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await GetAccessTokenForScopesAsync(_settings.DefaultScopes);
    }

    public async Task<string?> GetAccessTokenForScopesAsync(IEnumerable<string> scopes)
    {
        try
        {
            var tokenResult = await _tokenProvider.RequestAccessToken(
                new AccessTokenRequestOptions
                {
                    Scopes = scopes.ToArray()
                });

            if (tokenResult.TryGetToken(out var token))
            {
                return token.Value;
            }

            _logger.LogWarning(
                "AG ONE SSO: Failed to acquire token. Status: {Status}",
                tokenResult.Status);
            return null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            _logger.LogWarning(ex, "AG ONE SSO: Token not available");
            ex.Redirect();
            return null;
        }
    }

    public void NavigateToProduct(string productName, string? returnPath = null)
    {
        var productUrl = GetProductUrl(productName);
        if (string.IsNullOrEmpty(productUrl))
        {
            _logger.LogError("AG ONE SSO: Unknown product: {Product}", productName);
            return;
        }

        var targetUrl = returnPath != null
            ? $"{productUrl.TrimEnd('/')}/{returnPath.TrimStart('/')}"
            : productUrl;

        _logger.LogInformation(
            "AG ONE SSO: Navigating from {Current} to {Target}",
            _settings.ProductName, productName);

        _navigation.NavigateTo(targetUrl, forceLoad: true);
    }

    public void Login(string? returnUrl = null)
    {
        if (_settings.RedirectToPortalOnUnauthenticated
            && !string.IsNullOrEmpty(_settings.AgOnePortalUrl))
        {
            // Redirect to AG ONE Portal with return URL
            var currentUrl = returnUrl ?? _navigation.Uri;
            var portalLoginUrl = $"{_settings.AgOnePortalUrl.TrimEnd('/')}" +
                $"/authentication/login?returnUrl={Uri.EscapeDataString(currentUrl)}";

            _logger.LogInformation(
                "AG ONE SSO: Redirecting to Portal for login. Return URL: {ReturnUrl}",
                currentUrl);

            _navigation.NavigateTo(portalLoginUrl, forceLoad: true);
        }
        else
        {
            // This is the Portal itself, use MSAL directly
            var loginUrl = $"authentication/login";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                loginUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }
            _navigation.NavigateTo(loginUrl, forceLoad: true);
        }
    }

    public void Logout()
    {
        _logger.LogInformation(
            "AG ONE SSO: Logging out from {Product}", _settings.ProductName);

        // Navigate to the MSAL logout endpoint
        // This will clear the Entra ID session (SSO cookie) for all products
        _navigation.NavigateToLogout("authentication/logout",
            _settings.PostLogoutRedirectUri);
    }

    public async Task<bool> TrySilentLoginAsync()
    {
        try
        {
            var tokenResult = await _tokenProvider.RequestAccessToken(
                new AccessTokenRequestOptions
                {
                    Scopes = _settings.DefaultScopes.ToArray()
                });

            if (tokenResult.TryGetToken(out _))
            {
                _logger.LogDebug(
                    "AG ONE SSO: Silent login successful for {Product}",
                    _settings.ProductName);
                return true;
            }

            _logger.LogDebug(
                "AG ONE SSO: Silent login failed for {Product}. Status: {Status}",
                _settings.ProductName, tokenResult.Status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "AG ONE SSO: Silent login exception for {Product}",
                _settings.ProductName);
            return false;
        }
    }

    private async Task<ClaimsPrincipal?> GetUserAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true ? state.User : null;
    }

    private string? GetProductUrl(string productName)
    {
        return productName.ToLowerInvariant() switch
        {
            "portal" => _settings.Products.Portal,
            "learn" => _settings.Products.Learn,
            "safe" => _settings.Products.Safe,
            "work" => _settings.Products.Work,
            "pulse" => _settings.Products.Pulse,
            _ => null
        };
    }
}
