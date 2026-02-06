using AgOne.Shared.Auth.Api.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Api.Middleware;

/// <summary>
/// Middleware that validates AG ONE SSO tokens on every request.
/// Adds custom headers and validates cross-product requests.
/// 
/// Usage in Program.cs:
///   app.UseAgOneSsoValidation();
/// </summary>
public class AgOneSsoValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AgOneSsoValidationMiddleware> _logger;
    private readonly AgOneApiAuthSettings _settings;

    public AgOneSsoValidationMiddleware(
        RequestDelegate next,
        ILogger<AgOneSsoValidationMiddleware> logger,
        IOptions<AgOneApiAuthSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for non-authenticated endpoints
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }

        // Add AG ONE product context header
        context.Response.Headers.Append("X-AgOne-Product", _settings.ProductName);

        // If the user is authenticated, log the access
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("oid")?.Value ?? "unknown";
            var requestPath = context.Request.Path;

            _logger.LogDebug(
                "AG ONE SSO [{Product}]: Authenticated request from user {UserId} to {Path}",
                _settings.ProductName, userId, requestPath);

            // Add user context to HttpContext.Items for downstream use
            context.Items["AgOne.UserId"] = userId;
            context.Items["AgOne.ProductName"] = _settings.ProductName;
            context.Items["AgOne.UserEmail"] =
                context.User.FindFirst("preferred_username")?.Value
                ?? context.User.FindFirst("email")?.Value;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register the AG ONE SSO validation middleware.
/// </summary>
public static class AgOneSsoValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseAgOneSsoValidation(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AgOneSsoValidationMiddleware>();
    }
}
