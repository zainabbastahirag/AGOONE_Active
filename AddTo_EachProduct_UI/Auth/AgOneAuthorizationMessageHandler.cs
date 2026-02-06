using AgOne.Shared.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace AgOne.UI.Auth;

public class AgOneAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public AgOneAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IOptions<AgOneSsoSettings> ssoSettings)
        : base(provider, navigation)
    {
        var s = ssoSettings.Value;
        var urls = new List<string>();
        if (!string.IsNullOrEmpty(s.ApiBaseUrl)) urls.Add(s.ApiBaseUrl);
        urls.AddRange(s.Products.GetAllUrls().Where(u => !string.IsNullOrEmpty(u)));
        if (urls.Count > 0)
            ConfigureHandler(authorizedUrls: urls, scopes: s.DefaultScopes);
    }
}
