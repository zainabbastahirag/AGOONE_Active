using AgOne.Shared.Auth.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace AgOne.Shared.Auth.Api.Extensions;

/// <summary>
/// Extension methods to configure Azure Entra ID authentication for AG ONE API backends.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds AG ONE SSO authentication to the API backend.
    /// Configures JWT Bearer authentication with Microsoft Identity Web.
    /// 
    /// Call this in your API's Program.cs:
    ///   builder.Services.AddAgOneSsoApiAuthentication(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddAgOneSsoApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var ssoSettings = new AgOneApiAuthSettings();
        configuration.GetSection(AgOneApiAuthSettings.AgOneSsoSection).Bind(ssoSettings);
        services.Configure<AgOneApiAuthSettings>(
            configuration.GetSection(AgOneApiAuthSettings.AgOneSsoSection));

        // Check if this is B2C/External ID or regular Entra ID
        var isB2C = !string.IsNullOrEmpty(ssoSettings.SignUpSignInPolicyId);

        if (isB2C)
        {
            // Configure for Azure AD B2C / External ID
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(
                    jwtOptions =>
                    {
                        configuration.Bind(AgOneApiAuthSettings.AzureAdSection, jwtOptions);

                        jwtOptions.TokenValidationParameters.NameClaimType = "name";
                        jwtOptions.TokenValidationParameters.RoleClaimType = "roles";

                        // Allow tokens from any of the known valid issuers
                        if (ssoSettings.ValidIssuers.Count > 0)
                        {
                            jwtOptions.TokenValidationParameters.ValidIssuers =
                                ssoSettings.ValidIssuers;
                        }

                        jwtOptions.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine(
                                    $"AG ONE SSO API [{ssoSettings.ProductName}]: " +
                                    $"Authentication failed: {context.Exception.Message}");
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = context =>
                            {
                                Console.WriteLine(
                                    $"AG ONE SSO API [{ssoSettings.ProductName}]: " +
                                    $"Token validated for user: " +
                                    $"{context.Principal?.FindFirst("oid")?.Value}");
                                return Task.CompletedTask;
                            }
                        };
                    },
                    identityOptions =>
                    {
                        configuration.Bind(AgOneApiAuthSettings.AzureAdSection, identityOptions);
                    });
        }
        else
        {
            // Configure for regular Azure Entra ID
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(
                    jwtOptions =>
                    {
                        configuration.Bind(AgOneApiAuthSettings.AzureAdSection, jwtOptions);

                        jwtOptions.TokenValidationParameters.NameClaimType = "name";
                        jwtOptions.TokenValidationParameters.RoleClaimType = "roles";

                        if (ssoSettings.ValidIssuers.Count > 0)
                        {
                            jwtOptions.TokenValidationParameters.ValidIssuers =
                                ssoSettings.ValidIssuers;
                        }

                        if (!ssoSettings.ValidateIssuer)
                        {
                            jwtOptions.TokenValidationParameters.ValidateIssuer = false;
                        }

                        jwtOptions.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine(
                                    $"AG ONE SSO API [{ssoSettings.ProductName}]: " +
                                    $"Auth failed: {context.Exception.Message}");
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = context =>
                            {
                                Console.WriteLine(
                                    $"AG ONE SSO API [{ssoSettings.ProductName}]: " +
                                    $"Token validated for: " +
                                    $"{context.Principal?.FindFirst("preferred_username")?.Value}");
                                return Task.CompletedTask;
                            }
                        };
                    },
                    identityOptions =>
                    {
                        configuration.Bind(AgOneApiAuthSettings.AzureAdSection, identityOptions);
                    });
        }

        return services;
    }
}
