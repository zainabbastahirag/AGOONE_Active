using AgOne.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AgOne.API.Auth;

[ApiController]
[Route("api/agone-auth")]
[Authorize]
public class AgOneAuthController : ControllerBase
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly AgOneApiAuthSettings _settings;

    public AgOneAuthController(ITokenStorageService tokenStorage, IOptions<AgOneApiAuthSettings> settings)
    {
        _tokenStorage = tokenStorage;
        _settings = settings.Value;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] bool allProducts = true)
    {
        var userId = User.FindFirst("oid")?.Value;
        if (userId == null) return Unauthorized();
        if (allProducts) { await _tokenStorage.DeactivateAllUserTokensAsync(userId); await _tokenStorage.EndAllSessionsAsync(userId); }
        else await _tokenStorage.DeactivateTokenAsync(userId, _settings.ProductName);
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst("oid")?.Value;
        if (userId == null) return Unauthorized();
        var tokens = await _tokenStorage.GetAllUserTokensAsync(userId);
        var sessions = await _tokenStorage.GetActiveSessionsAsync(userId);
        return Ok(new
        {
            userId,
            email = User.FindFirst("preferred_username")?.Value,
            displayName = User.FindFirst("name")?.Value,
            currentProduct = _settings.ProductName,
            activeProducts = tokens.Select(t => new { t.ProductName, t.AccessTokenExpiresUtc, t.RefreshCount }),
            activeSessions = sessions.Select(s => new { s.SessionId, s.ProductsAccessed, s.StartedUtc, s.LastActivityUtc })
        });
    }

    [HttpGet("validate")]
    public async Task<IActionResult> Validate()
    {
        var userId = User.FindFirst("oid")?.Value;
        if (userId == null) return Unauthorized();
        return Ok(new { isValid = true, hasStoredToken = await _tokenStorage.HasValidTokenAsync(userId, _settings.ProductName) });
    }
}
