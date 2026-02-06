using System.Security.Claims;
using AgOne.Shared.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.UI.Auth;

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
        return user?.FindFirst("name")?.Value ?? user?.FindFirst("preferred_username")?.Value;
    }

    public async Task<string?> GetUserEmailAsync()
    {
        var user = await GetUserAsync();
        return user?.FindFirst("preferred_username")?.Value ?? user?.FindFirst("email")?.Value;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var user = await GetUserAsync();
        return user?.FindFirst("oid")?.Value ?? user?.FindFirst("sub")?.Value;
    }

    public async Task<string?> GetAccessTokenAsync() =>
        await GetAccessTokenForScopesAsync(_settings.DefaultScopes);

    public async Task<string?> GetAccessTokenForScopesAsync(IEnumerable<string> scopes)
    {
        try
        {
            var result = await _tokenProvider.RequestAccessToken(
                new AccessTokenRequestOptions { Scopes = scopes.ToArray() });
            return result.TryGetToken(out var token) ? token.Value : null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    public void NavigateToProduct(string productName, string? returnPath = null)
    {
        var url = productName.ToLowerInvariant() switch
        {
            "portal" => _settings.Products.Portal,
            "learn" => _settings.Products.Learn,
            "safe" => _settings.Products.Safe,
            "work" => _settings.Products.Work,
            "pulse" => _settings.Products.Pulse,
            _ => null
        };
        if (url == null) return;
        var target = returnPath != null ? $"{url.TrimEnd('/')}/{returnPath.TrimStart('/')}" : url;
        _navigation.NavigateTo(target, forceLoad: true);
    }

    public void Login(string? returnUrl = null)
    {
        if (_settings.RedirectToPortalOnUnauthenticated && !string.IsNullOrEmpty(_settings.AgOnePortalUrl))
        {
            var current = returnUrl ?? _navigation.Uri;
            _navigation.NavigateTo(
                $"{_settings.AgOnePortalUrl.TrimEnd('/')}/authentication/login?returnUrl={Uri.EscapeDataString(current)}",
                forceLoad: true);
        }
        else
        {
            var loginUrl = "authentication/login";
            if (!string.IsNullOrEmpty(returnUrl))
                loginUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
            _navigation.NavigateTo(loginUrl, forceLoad: true);
        }
    }

    public void Logout() =>
        _navigation.NavigateToLogout("authentication/logout", _settings.PostLogoutRedirectUri);

    public async Task<bool> TrySilentLoginAsync()
    {
        try
        {
            var result = await _tokenProvider.RequestAccessToken(
                new AccessTokenRequestOptions { Scopes = _settings.DefaultScopes.ToArray() });
            return result.TryGetToken(out _);
        }
        catch { return false; }
    }

    private async Task<ClaimsPrincipal?> GetUserAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true ? state.User : null;
    }
}
