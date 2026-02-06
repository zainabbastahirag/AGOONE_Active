// ============================================================================
// Example Protected Controller
// ============================================================================
// Shows how to use AG ONE SSO authorization policies on your existing
// controllers. Just add the [Authorize] attributes.
// ============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgOne.Learn.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AgOne.Authenticated")] // All endpoints require AG ONE auth
public class SampleProtectedController : ControllerBase
{
    /// <summary>
    /// Example: Any authenticated AG ONE user can access this.
    /// </summary>
    [HttpGet("public-data")]
    public IActionResult GetPublicData()
    {
        var userId = User.FindFirst("oid")?.Value;
        var email = User.FindFirst("preferred_username")?.Value;

        return Ok(new
        {
            Message = "You are authenticated via AG ONE SSO!",
            UserId = userId,
            Email = email,
            Product = "Learn",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Example: Only users with Learn product access can use this.
    /// Uses the product-specific policy.
    /// </summary>
    [HttpGet("learn-data")]
    [Authorize(Policy = "AgOne.Learn.Access")]
    public IActionResult GetLearnSpecificData()
    {
        return Ok(new
        {
            Message = "You have access to AG ONE Learn!",
            Courses = new[] { "Course 1", "Course 2", "Course 3" }
        });
    }

    /// <summary>
    /// Example: Admin-only endpoint.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "AgOne.Admin")]
    public IActionResult GetAdminData()
    {
        return Ok(new { Message = "You are an AG ONE Admin!" });
    }

    /// <summary>
    /// Example: Getting user info from the SSO token claims.
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetMyInfo()
    {
        return Ok(new
        {
            UserId = User.FindFirst("oid")?.Value,
            Email = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst("email")?.Value,
            Name = User.FindFirst("name")?.Value,
            Roles = User.FindAll("roles").Select(c => c.Value).ToList(),
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }
}
