using AgOne.Shared.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgOne.UI.Auth;

public static class SsoServiceCollectionExtensions
{
    /// <summary>
    /// Call in Program.cs: builder.Services.AddAgOneSso(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddAgOneSso(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var ssoSettings = new AgOneSsoSettings();
        configuration.GetSection(AgOneSsoSettings.SectionName).Bind(ssoSettings);
        services.Configure<AgOneSsoSettings>(configuration.GetSection(AgOneSsoSettings.SectionName));

        services.AddMsalAuthentication(options =>
        {
            options.ProviderOptions.Authentication.Authority = ssoSettings.Authority;
            options.ProviderOptions.Authentication.ClientId = ssoSettings.ClientId;
            options.ProviderOptions.Authentication.PostLogoutRedirectUri = ssoSettings.PostLogoutRedirectUri;

            if (!string.IsNullOrEmpty(ssoSettings.SignUpSignInPolicyId))
            {
                options.ProviderOptions.Authentication.Authority =
                    $"{ssoSettings.Authority}/{ssoSettings.SignUpSignInPolicyId}";
            }

            options.AuthenticationPaths.LogInCallbackPath = ssoSettings.LoginCallbackPath;
            options.AuthenticationPaths.LogOutCallbackPath = ssoSettings.LogoutCallbackPath;

            foreach (var scope in ssoSettings.DefaultScopes)
                options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);

            foreach (var scope in ssoSettings.AdditionalScopes)
                options.ProviderOptions.AdditionalScopesToConsent.Add(scope);

            options.ProviderOptions.LoginMode = "redirect";
            options.ProviderOptions.Cache.CacheLocation = "localStorage";
            options.ProviderOptions.Cache.StoreAuthStateInCookie = true;
        });

        services.AddScoped<AgOneSsoService>();
        services.AddScoped<IAgOneSsoService>(sp => sp.GetRequiredService<AgOneSsoService>());
        services.AddScoped<AgOneAuthorizationMessageHandler>();

        if (!string.IsNullOrEmpty(ssoSettings.ApiBaseUrl))
        {
            services.AddHttpClient("AgOne.ProductApi", client =>
                client.BaseAddress = new Uri(ssoSettings.ApiBaseUrl))
                .AddHttpMessageHandler<AgOneAuthorizationMessageHandler>();

            services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("AgOne.ProductApi"));
        }

        return services;
    }
}
