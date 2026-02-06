using System.IdentityModel.Tokens.Jwt;
using AgOne.Shared.Auth.Api.Configuration;
using AgOne.Shared.Auth.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Api.Middleware;

/// <summary>
/// Middleware that captures access tokens from incoming API requests
/// and stores them in the database automatically.
/// 
/// When the Blazor WASM client sends an API request with a Bearer token,
/// this middleware extracts the token, parses its claims, and saves it
/// to the database via ITokenStorageService.
/// 
/// This is how tokens get INTO your database - the client sends them
/// on every API call, and this middleware stores/updates them.
/// 
/// Usage in Program.cs:
///   app.UseTokenCapture(); // After UseAuthentication + UseAuthorization
/// </summary>
public class TokenCaptureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenCaptureMiddleware> _logger;
    private readonly AgOneApiAuthSettings _settings;

    public TokenCaptureMiddleware(
        RequestDelegate next,
        ILogger<TokenCaptureMiddleware> logger,
        IOptions<AgOneApiAuthSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only capture tokens for authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                await CaptureAndStoreTokenAsync(context);
            }
            catch (Exception ex)
            {
                // Token capture failure should not break the request pipeline
                _logger.LogWarning(ex,
                    "AG ONE TokenCapture: Failed to capture/store token. " +
                    "Request continues normally.");
            }
        }

        await _next(context);
    }

    private async Task CaptureAndStoreTokenAsync(HttpContext context)
    {
        // Extract the Bearer token from the Authorization header
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var accessToken = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        // Extract user info from the already-validated claims
        var userId = context.User.FindFirst("oid")?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogDebug("AG ONE TokenCapture: No user ID (oid/sub) claim found. Skipping.");
            return;
        }

        // Parse the JWT to get expiration (don't validate - it's already validated by auth middleware)
        DateTime expiresUtc;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            expiresUtc = jwt.ValidTo;
        }
        catch
        {
            // If we can't parse the JWT, use a default expiry of 1 hour
            expiresUtc = DateTime.UtcNow.AddHours(1);
        }

        var email = context.User.FindFirst("preferred_username")?.Value
            ?? context.User.FindFirst("email")?.Value;
        var displayName = context.User.FindFirst("name")?.Value;
        var tenantId = context.User.FindFirst("tid")?.Value;
        var scopes = context.User.FindFirst("scp")?.Value;

        // Get the token storage service from DI (scoped)
        var tokenStorage = context.RequestServices.GetService<ITokenStorageService>();
        if (tokenStorage == null)
        {
            _logger.LogDebug("AG ONE TokenCapture: ITokenStorageService not registered. Skipping.");
            return;
        }

        // Check if we also received a refresh token
        // (The Blazor client sends it via a custom header when available)
        var refreshToken = context.Request.Headers["X-AgOne-Refresh-Token"].ToString();
        if (string.IsNullOrEmpty(refreshToken))
        {
            refreshToken = null;
        }

        var idToken = context.Request.Headers["X-AgOne-Id-Token"].ToString();
        if (string.IsNullOrEmpty(idToken))
        {
            idToken = null;
        }

        // Save the token to the database
        await tokenStorage.SaveTokenAsync(new SaveTokenRequest
        {
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            ProductName = _settings.ProductName,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IdToken = idToken,
            AccessTokenExpiresUtc = expiresUtc,
            GrantedScopes = scopes,
            TenantId = tenantId,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString()
        });

        // Also track the session
        await tokenStorage.TrackSessionAsync(new TrackSessionRequest
        {
            UserId = userId,
            Email = email,
            ProductName = _settings.ProductName,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString()
        });
    }
}

/// <summary>
/// Extension method to register the token capture middleware.
/// </summary>
public static class TokenCaptureMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that captures Bearer tokens from API requests
    /// and stores them in the database.
    /// 
    /// Place AFTER UseAuthentication() and UseAuthorization():
    /// 
    ///   app.UseAuthentication();
    ///   app.UseAuthorization();
    ///   app.UseTokenCapture();    // <-- Add here
    /// </summary>
    public static IApplicationBuilder UseTokenCapture(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenCaptureMiddleware>();
    }
}
