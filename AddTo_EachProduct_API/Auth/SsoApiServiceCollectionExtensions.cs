using AgOne.Shared.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace AgOne.API.Auth;

public static class SsoApiServiceCollectionExtensions
{
    /// <summary>
    /// Call in Program.cs: builder.Services.AddAgOneSsoApi(builder.Configuration);
    /// This single method sets up authentication, authorization, and CORS.
    /// </summary>
    public static IServiceCollection AddAgOneSsoApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var ssoSettings = new AgOneApiAuthSettings();
        configuration.GetSection(AgOneApiAuthSettings.AgOneSsoSection).Bind(ssoSettings);
        services.Configure<AgOneApiAuthSettings>(
            configuration.GetSection(AgOneApiAuthSettings.AgOneSsoSection));

        // ── Authentication ──
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(
                jwtOptions =>
                {
                    configuration.Bind(AgOneApiAuthSettings.AzureAdSection, jwtOptions);
                    jwtOptions.TokenValidationParameters.NameClaimType = "name";
                    jwtOptions.TokenValidationParameters.RoleClaimType = "roles";
                    if (ssoSettings.ValidIssuers.Count > 0)
                        jwtOptions.TokenValidationParameters.ValidIssuers = ssoSettings.ValidIssuers;
                    if (!ssoSettings.ValidateIssuer)
                        jwtOptions.TokenValidationParameters.ValidateIssuer = false;
                },
                identityOptions =>
                {
                    configuration.Bind(AgOneApiAuthSettings.AzureAdSection, identityOptions);
                });

        // ── Authorization ──
        services.AddSingleton<IAuthorizationHandler, ProductAccessHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy("AgOne.Authenticated", p => p.RequireAuthenticatedUser())
            .AddPolicy("AgOne.ValidUser", p => { p.RequireAuthenticatedUser(); p.RequireClaim("oid"); })
            .AddPolicy("AgOne.Portal.Access", p => { p.RequireAuthenticatedUser(); p.AddRequirements(new ProductAccessRequirement("Portal")); })
            .AddPolicy("AgOne.Learn.Access", p => { p.RequireAuthenticatedUser(); p.AddRequirements(new ProductAccessRequirement("Learn")); })
            .AddPolicy("AgOne.Safe.Access", p => { p.RequireAuthenticatedUser(); p.AddRequirements(new ProductAccessRequirement("Safe")); })
            .AddPolicy("AgOne.Work.Access", p => { p.RequireAuthenticatedUser(); p.AddRequirements(new ProductAccessRequirement("Work")); })
            .AddPolicy("AgOne.Pulse.Access", p => { p.RequireAuthenticatedUser(); p.AddRequirements(new ProductAccessRequirement("Pulse")); })
            .AddPolicy("AgOne.Admin", p => { p.RequireAuthenticatedUser(); p.RequireRole("Admin", "GlobalAdmin"); });

        // ── CORS ──
        services.AddCors(options =>
        {
            options.AddPolicy("AgOneCors", builder =>
            {
                if (ssoSettings.AllowedOrigins.Count > 0)
                    builder.WithOrigins(ssoSettings.AllowedOrigins.ToArray())
                        .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                else
                    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        // ── HttpClient for Entra ID token refresh ──
        services.AddHttpClient("AgOne.EntraId", c => c.Timeout = TimeSpan.FromSeconds(30));

        return services;
    }
}

// ── Authorization requirement + handler ──

public class ProductAccessRequirement : IAuthorizationRequirement
{
    public string ProductName { get; }
    public ProductAccessRequirement(string productName) => ProductName = productName;
}

public class ProductAccessHandler : AuthorizationHandler<ProductAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ProductAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
