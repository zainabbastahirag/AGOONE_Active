using AgOne.Shared.Auth.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Handlers;

/// <summary>
/// HTTP message handler that automatically attaches the access token
/// to outgoing API requests. Handles token refresh and redirects to
/// login when tokens cannot be acquired.
/// </summary>
public class AgOneAuthorizationMessageHandler : AuthorizationMessageHandler
{
    private bool _isConfigured;

    public AgOneAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IOptions<AgOneSsoSettings> ssoSettings)
        : base(provider, navigation)
    {
        var settings = ssoSettings.Value;

        // Configure authorized URLs - the handler will only attach tokens
        // to requests going to these URLs
        var authorizedUrls = new List<string>();

        if (!string.IsNullOrEmpty(settings.ApiBaseUrl))
        {
            authorizedUrls.Add(settings.ApiBaseUrl);
        }

        // Add all product URLs as authorized (for cross-product API calls)
        authorizedUrls.AddRange(settings.Products.GetAllUrls()
            .Where(u => !string.IsNullOrEmpty(u)));

        if (authorizedUrls.Count > 0)
        {
            ConfigureHandler(
                authorizedUrls: authorizedUrls.ToArray(),
                scopes: settings.DefaultScopes.ToArray());
            _isConfigured = true;
        }
    }

    /// <summary>
    /// Allows reconfiguring the handler for specific cross-product API calls.
    /// </summary>
    public void ConfigureHandler(string[] authorizedUrls, string[] scopes)
    {
        if (!_isConfigured)
        {
            ConfigureHandler(
                authorizedUrls: authorizedUrls,
                scopes: scopes);
            _isConfigured = true;
        }
    }
}
