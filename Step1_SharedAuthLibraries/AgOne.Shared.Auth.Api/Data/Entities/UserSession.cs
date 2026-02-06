using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgOne.Shared.Auth.Api.Data.Entities;

/// <summary>
/// Tracks user SSO sessions across AG ONE products.
/// One record per login session. Updated when user navigates between products.
/// Useful for auditing, "active sessions" UI, and forced logout.
/// 
/// *** DATABASE TABLE: UserSessions ***
/// </summary>
[Table("UserSessions")]
public class UserSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Unique session identifier (GUID).
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User's Entra ID Object ID (OID).
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// Which product initiated the session (usually "Portal").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InitiatedByProduct { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of products the user has accessed during this session.
    /// Example: "Portal,Learn,Safe"
    /// </summary>
    [MaxLength(512)]
    public string? ProductsAccessed { get; set; }

    /// <summary>
    /// Session start time.
    /// </summary>
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity time (updated on every API call).
    /// </summary>
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Session end time (set on logout or expiry).
    /// </summary>
    public DateTime? EndedUtc { get; set; }

    /// <summary>
    /// Whether the session is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// IP address at session start.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent at session start.
    /// </summary>
    [MaxLength(512)]
    public string? UserAgent { get; set; }
}
