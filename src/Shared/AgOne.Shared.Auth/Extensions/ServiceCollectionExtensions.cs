using AgOne.Shared.Auth.Configuration;
using AgOne.Shared.Auth.Handlers;
using AgOne.Shared.Auth.Providers;
using AgOne.Shared.Auth.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgOne.Shared.Auth.Extensions;

/// <summary>
/// Extension methods to register AG ONE SSO services in the Blazor WASM DI container.
/// Call this in your product's Program.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AG ONE SSO authentication using Azure Entra ID / External ID.
    /// This configures MSAL, auth state provider, HTTP handlers, and SSO services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app configuration (from appsettings.json).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgOneSso(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var ssoSettings = new AgOneSsoSettings();
        configuration.GetSection(AgOneSsoSettings.SectionName).Bind(ssoSettings);
        services.Configure<AgOneSsoSettings>(configuration.GetSection(AgOneSsoSettings.SectionName));

        // Register MSAL authentication
        services.AddMsalAuthentication(options =>
        {
            // Provider options (MSAL configuration)
            options.ProviderOptions.Authentication.Authority = ssoSettings.Authority;
            options.ProviderOptions.Authentication.ClientId = ssoSettings.ClientId;
            options.ProviderOptions.Authentication.PostLogoutRedirectUri = ssoSettings.PostLogoutRedirectUri;

            // If using B2C / External ID with user flows
            if (!string.IsNullOrEmpty(ssoSettings.SignUpSignInPolicyId))
            {
                options.ProviderOptions.Authentication.Authority =
                    $"{ssoSettings.Authority}/{ssoSettings.SignUpSignInPolicyId}";
            }

            // Configure login/logout paths
            options.AuthenticationPaths.LogInCallbackPath = ssoSettings.LoginCallbackPath;
            options.AuthenticationPaths.LogOutCallbackPath = ssoSettings.LogoutCallbackPath;

            // Add default scopes
            foreach (var scope in ssoSettings.DefaultScopes)
            {
                options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
            }

            // Add additional scopes for cross-product API access
            foreach (var scope in ssoSettings.AdditionalScopes)
            {
                options.ProviderOptions.AdditionalScopesToConsent.Add(scope);
            }

            // Enable SSO with prompt=none for silent auth
            options.ProviderOptions.LoginMode = "redirect";

            // Cache configuration
            options.ProviderOptions.Cache.CacheLocation = "localStorage";
            options.ProviderOptions.Cache.StoreAuthStateInCookie = true;
        });

        // Register custom auth state provider
        services.AddScoped<AgOneAuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<AgOneAuthStateProvider>());

        // Register SSO service
        services.AddScoped<IAgOneSsoService, AgOneSsoService>();

        // Register the authorization message handler
        services.AddScoped<AgOneAuthorizationMessageHandler>();

        // Configure the named HTTP client for the product's own API
        if (!string.IsNullOrEmpty(ssoSettings.ApiBaseUrl))
        {
            services.AddHttpClient("AgOne.ProductApi", client =>
            {
                client.BaseAddress = new Uri(ssoSettings.ApiBaseUrl);
            })
            .AddHttpMessageHandler<AgOneAuthorizationMessageHandler>();

            // Also register a typed HttpClient via the factory
            services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient("AgOne.ProductApi"));
        }

        return services;
    }

    /// <summary>
    /// Adds a named HTTP client for cross-product API calls.
    /// Each product API you need to call should have its own named client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the HTTP client (e.g., "AgOne.LearnApi").</param>
    /// <param name="baseUrl">The base URL of the product API.</param>
    /// <param name="scopes">The scopes required for this API.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgOneProductApiClient(
        this IServiceCollection services,
        string clientName,
        string baseUrl,
        params string[] scopes)
    {
        services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler(sp =>
        {
            var handler = sp.GetRequiredService<AgOneAuthorizationMessageHandler>();
            handler.ConfigureHandler(
                authorizedUrls: new[] { baseUrl },
                scopes: scopes);
            return handler;
        });

        return services;
    }
}
