using AgOne.Shared.Auth.Api.Configuration;
using AgOne.Shared.Auth.Api.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgOne.Shared.Auth.Api.Extensions;

/// <summary>
/// Extension methods to configure authorization policies for AG ONE products.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds AG ONE authorization policies.
    /// These policies can be used on controllers/endpoints to enforce
    /// product-specific access rules.
    /// 
    /// Call this in your API's Program.cs:
    ///   builder.Services.AddAgOneSsoApiAuthorization(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddAgOneSsoApiAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the custom authorization handler
        services.AddSingleton<IAuthorizationHandler, ProductAccessRequirementHandler>();

        services.AddAuthorizationBuilder()
            // Policy: User must be authenticated (basic)
            .AddPolicy("AgOne.Authenticated", policy =>
            {
                policy.RequireAuthenticatedUser();
            })

            // Policy: User must have a valid OID (Object ID) claim
            .AddPolicy("AgOne.ValidUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("oid");
            })

            // Product-specific access policies
            // These can be extended with custom roles/claims from your Entra ID
            .AddPolicy("AgOne.Portal.Access", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ProductAccessRequirement("Portal"));
            })
            .AddPolicy("AgOne.Learn.Access", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ProductAccessRequirement("Learn"));
            })
            .AddPolicy("AgOne.Safe.Access", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ProductAccessRequirement("Safe"));
            })
            .AddPolicy("AgOne.Work.Access", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ProductAccessRequirement("Work"));
            })
            .AddPolicy("AgOne.Pulse.Access", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ProductAccessRequirement("Pulse"));
            })

            // Role-based policies (map to Azure AD App Roles)
            .AddPolicy("AgOne.Admin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin", "GlobalAdmin");
            })
            .AddPolicy("AgOne.Manager", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin", "GlobalAdmin", "Manager");
            });

        return services;
    }
}

/// <summary>
/// Custom authorization requirement for product-specific access.
/// Can be extended to check product licenses, subscriptions, etc.
/// </summary>
public class ProductAccessRequirement : IAuthorizationRequirement
{
    public string ProductName { get; }

    public ProductAccessRequirement(string productName)
    {
        ProductName = productName;
    }
}
