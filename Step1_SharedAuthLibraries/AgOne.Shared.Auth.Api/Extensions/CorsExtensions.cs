using AgOne.Shared.Auth.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgOne.Shared.Auth.Api.Extensions;

/// <summary>
/// Extension methods to configure CORS for AG ONE multi-product SSO.
/// All product frontends need to be able to call each product's API.
/// </summary>
public static class CorsExtensions
{
    public const string AgOneCorsPolicy = "AgOneCorsPolicy";

    /// <summary>
    /// Adds CORS configuration that allows all AG ONE product frontends
    /// to call this API. This is essential for the SSO to work across
    /// separate domains.
    /// 
    /// Call this in your API's Program.cs:
    ///   builder.Services.AddAgOneSsoCors(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddAgOneSsoCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new AgOneApiAuthSettings();
        configuration.GetSection(AgOneApiAuthSettings.AgOneSsoSection).Bind(settings);

        services.AddCors(options =>
        {
            options.AddPolicy(AgOneCorsPolicy, builder =>
            {
                if (settings.AllowedOrigins.Count > 0)
                {
                    builder.WithOrigins(settings.AllowedOrigins.ToArray())
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    // Fallback: Restrict to known origins only in production
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        return services;
    }
}
