using System.IdentityModel.Tokens.Jwt;
using AgOne.Shared.Auth.Api.Configuration;
using AgOne.Shared.Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgOne.Shared.Auth.Api.Controllers;

/// <summary>
/// API Controller for AG ONE SSO token management.
/// 
/// *** COPY THIS CONTROLLER TO EACH PRODUCT'S API PROJECT ***
/// 
/// Or, if you're using the shared library as a project reference,
/// make sure your API project scans this assembly for controllers:
///   builder.Services.AddControllers()
///       .AddApplicationPart(typeof(AgOneAuthController).Assembly);
/// 
/// Endpoints:
///   POST /api/agone-auth/sync-token   - Receives tokens from Blazor client for DB storage
///   POST /api/agone-auth/logout       - Notifies backend of user logout
///   GET  /api/agone-auth/me           - Returns current user info from stored token
///   GET  /api/agone-auth/sessions     - Returns active sessions for current user
///   POST /api/agone-auth/refresh      - Manually triggers a token refresh
///   GET  /api/agone-auth/validate     - Validates the current session is active
/// </summary>
[ApiController]
[Route("api/agone-auth")]
[Authorize(Policy = "AgOne.Authenticated")]
public class AgOneAuthController : ControllerBase
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly AgOneApiAuthSettings _settings;
    private readonly ILogger<AgOneAuthController> _logger;

    public AgOneAuthController(
        ITokenStorageService tokenStorage,
        IOptions<AgOneApiAuthSettings> settings,
        ILogger<AgOneAuthController> logger)
    {
        _tokenStorage = tokenStorage;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Receives an access token from the Blazor WASM client and stores it in the DB.
    /// Called by the client-side TokenSyncService after authentication.
    /// </summary>
    [HttpPost("sync-token")]
    public async Task<IActionResult> SyncToken([FromBody] SyncTokenRequestDto request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        DateTime expiresUtc;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(request.AccessToken);
            expiresUtc = jwt.ValidTo;
        }
        catch
        {
            expiresUtc = request.ExpiresAt?.UtcDateTime ?? DateTime.UtcNow.AddHours(1);
        }

        await _tokenStorage.SaveTokenAsync(new SaveTokenRequest
        {
            UserId = userId,
            Email = GetUserEmail(),
            DisplayName = GetUserDisplayName(),
            ProductName = request.ProductName ?? _settings.ProductName,
            AccessToken = request.AccessToken,
            RefreshToken = request.RefreshToken,
            IdToken = request.IdToken,
            AccessTokenExpiresUtc = expiresUtc,
            GrantedScopes = request.GrantedScopes,
            TenantId = User.FindFirst("tid")?.Value,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });

        return Ok(new { message = "Token stored successfully", userId });
    }

    /// <summary>
    /// Handles logout - deactivates stored tokens and ends the session.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] bool allProducts = true)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (allProducts)
        {
            await _tokenStorage.DeactivateAllUserTokensAsync(userId);
            await _tokenStorage.EndAllSessionsAsync(userId);
        }
        else
        {
            await _tokenStorage.DeactivateTokenAsync(userId, _settings.ProductName);
        }

        _logger.LogInformation(
            "AG ONE Auth: Logout for user {UserId}, allProducts={AllProducts}",
            userId, allProducts);

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Returns current user information from the stored token and claims.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var tokens = await _tokenStorage.GetAllUserTokensAsync(userId);
        var sessions = await _tokenStorage.GetActiveSessionsAsync(userId);

        return Ok(new
        {
            userId,
            email = GetUserEmail(),
            displayName = GetUserDisplayName(),
            currentProduct = _settings.ProductName,
            activeProducts = tokens.Select(t => new
            {
                t.ProductName,
                tokenExpires = t.AccessTokenExpiresUtc,
                hasRefreshToken = !string.IsNullOrEmpty(t.RefreshToken),
                t.RefreshCount,
                lastUpdated = t.UpdatedUtc
            }),
            activeSessions = sessions.Select(s => new
            {
                s.SessionId,
                s.ProductsAccessed,
                started = s.StartedUtc,
                lastActivity = s.LastActivityUtc
            })
        });
    }

    /// <summary>
    /// Returns all active sessions for the current user.
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var sessions = await _tokenStorage.GetActiveSessionsAsync(userId);

        return Ok(sessions.Select(s => new
        {
            s.SessionId,
            s.InitiatedByProduct,
            s.ProductsAccessed,
            started = s.StartedUtc,
            lastActivity = s.LastActivityUtc,
            s.IpAddress
        }));
    }

    /// <summary>
    /// Manually triggers a token refresh using the stored refresh token.
    /// Returns the new access token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromQuery] string? productName)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var product = productName ?? _settings.ProductName;
        var result = await _tokenStorage.RefreshTokenAsync(userId, product);

        if (result.Success)
        {
            return Ok(new
            {
                message = "Token refreshed successfully",
                expiresUtc = result.ExpiresUtc,
                // Don't return the actual tokens in the response for security
                // The new tokens are already stored in the DB
            });
        }

        return BadRequest(new
        {
            message = "Token refresh failed",
            error = result.ErrorMessage
        });
    }

    /// <summary>
    /// Validates that the current user has an active, valid token stored.
    /// Useful for health checks and session validation from the client.
    /// </summary>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidateSession()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var hasValidToken = await _tokenStorage.HasValidTokenAsync(
            userId, _settings.ProductName);

        return Ok(new
        {
            isValid = true, // If we got here, JWT is valid
            hasStoredToken = hasValidToken,
            userId,
            product = _settings.ProductName
        });
    }

    // Helper methods
    private string? GetUserId() =>
        User.FindFirst("oid")?.Value ?? User.FindFirst("sub")?.Value;

    private string? GetUserEmail() =>
        User.FindFirst("preferred_username")?.Value ?? User.FindFirst("email")?.Value;

    private string? GetUserDisplayName() =>
        User.FindFirst("name")?.Value;
}

/// <summary>
/// DTO for the sync-token endpoint.
/// </summary>
public class SyncTokenRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? GrantedScopes { get; set; }
    public string? ProductName { get; set; }
}
