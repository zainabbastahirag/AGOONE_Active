using AgOne.Shared.Auth.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace AgOne.Shared.Auth.Api.Handlers;

/// <summary>
/// Authorization handler for product-specific access requirements.
/// Checks if the authenticated user has access to the requested product.
/// 
/// This handler can be customized to:
/// - Check product licenses/subscriptions from your database
/// - Validate product-specific roles from Entra ID claims
/// - Check group memberships
/// - Enforce time-based access rules
/// </summary>
public class ProductAccessRequirementHandler
    : AuthorizationHandler<ProductAccessRequirement>
{
    private readonly ILogger<ProductAccessRequirementHandler> _logger;

    public ProductAccessRequirementHandler(
        ILogger<ProductAccessRequirementHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProductAccessRequirement requirement)
    {
        // The user must be authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning(
                "AG ONE Auth: Unauthenticated user attempted to access {Product}",
                requirement.ProductName);
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirst("oid")?.Value
            ?? context.User.FindFirst("sub")?.Value;
        var userEmail = context.User.FindFirst("preferred_username")?.Value
            ?? context.User.FindFirst("email")?.Value;

        // ============================================================
        // CUSTOMIZE THIS SECTION FOR YOUR BUSINESS LOGIC
        // ============================================================
        // 
        // Option 1: All authenticated users can access all products
        //           (simplest - just being logged in via AG ONE is enough)
        //
        // Option 2: Check product-specific roles in Entra ID
        //           e.g., context.User.IsInRole($"{requirement.ProductName}.User")
        //
        // Option 3: Check product licenses from your database
        //           e.g., await _licenseService.HasProductAccess(userId, requirement.ProductName)
        //
        // Option 4: Check Entra ID group memberships
        //           e.g., context.User.Claims.Any(c => c.Type == "groups" && c.Value == productGroupId)
        //
        // Current implementation: Option 1 - All authenticated users have access
        // ============================================================

        // OPTION 1: All authenticated AG ONE users can access all products
        _logger.LogInformation(
            "AG ONE Auth: User {UserId} ({Email}) granted access to {Product}",
            userId, userEmail, requirement.ProductName);
        context.Succeed(requirement);

        // OPTION 2: Role-based access (uncomment to use)
        // var requiredRole = $"{requirement.ProductName}.User";
        // if (context.User.IsInRole(requiredRole) || context.User.IsInRole("GlobalAdmin"))
        // {
        //     context.Succeed(requirement);
        // }
        // else
        // {
        //     _logger.LogWarning(
        //         "AG ONE Auth: User {UserId} denied access to {Product} (missing role: {Role})",
        //         userId, requirement.ProductName, requiredRole);
        // }

        return Task.CompletedTask;
    }
}
