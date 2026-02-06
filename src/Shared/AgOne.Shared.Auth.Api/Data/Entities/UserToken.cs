using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgOne.Shared.Auth.Api.Data.Entities;

/// <summary>
/// Entity to store user tokens (access + refresh) in the database.
/// One record per user per product. Updated on every token refresh.
/// 
/// *** DATABASE TABLE: UserTokens ***
/// 
/// This entity should be added to your existing DbContext.
/// If you use EF Core migrations, add a migration after including this.
/// </summary>
[Table("UserTokens")]
public class UserToken
{
    /// <summary>
    /// Primary key - auto-generated.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// The user's Object ID (OID) from Azure Entra ID.
    /// This is the unique user identifier across all AG ONE products.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address (from preferred_username or email claim).
    /// Stored for easy lookup and debugging.
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Which product this token was issued for (Portal, Learn, Safe, Work, Pulse).
    /// A user may have separate tokens for each product.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// The encrypted access token (JWT).
    /// Access tokens are short-lived (typically 1 hour).
    /// The API uses this to make downstream calls on behalf of the user.
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The encrypted refresh token.
    /// Refresh tokens are long-lived (typically 24h to 90 days).
    /// Used to obtain new access tokens without user interaction.
    /// 
    /// NOTE: In Blazor WASM + Entra ID SPA flow, refresh tokens
    /// may not always be available. See the documentation for details.
    /// When not available, this will be empty and SSO session cookies
    /// handle silent token renewal instead.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// The ID token (JWT) containing user claims.
    /// </summary>
    public string? IdToken { get; set; }

    /// <summary>
    /// UTC timestamp when the access token expires.
    /// </summary>
    public DateTime AccessTokenExpiresUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the refresh token expires.
    /// </summary>
    public DateTime? RefreshTokenExpiresUtc { get; set; }

    /// <summary>
    /// The scopes that were granted with this token.
    /// Stored as a space-separated string.
    /// </summary>
    [MaxLength(1024)]
    public string? GrantedScopes { get; set; }

    /// <summary>
    /// The Entra ID tenant ID this token was issued from.
    /// </summary>
    [MaxLength(128)]
    public string? TenantId { get; set; }

    /// <summary>
    /// Whether this token record is currently active/valid.
    /// Set to false on logout or when revoked.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// UTC timestamp when this record was first created.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when this record was last updated (token refreshed).
    /// </summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The IP address of the client when the token was last issued/refreshed.
    /// Useful for security auditing.
    /// </summary>
    [MaxLength(45)]
    public string? LastIpAddress { get; set; }

    /// <summary>
    /// The User-Agent of the client when the token was last issued/refreshed.
    /// </summary>
    [MaxLength(512)]
    public string? LastUserAgent { get; set; }

    /// <summary>
    /// Number of times this token has been refreshed.
    /// </summary>
    public int RefreshCount { get; set; } = 0;
}
