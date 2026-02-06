using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgOne.Infrastructure.Auth.Entities;

[Table("UserTokens")]
public class UserToken
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required] [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(256)] public string? Email { get; set; }
    [MaxLength(256)] public string? DisplayName { get; set; }

    [Required] [MaxLength(50)]
    public string ProductName { get; set; } = string.Empty;

    [Required] public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public DateTime AccessTokenExpiresUtc { get; set; }
    public DateTime? RefreshTokenExpiresUtc { get; set; }

    [MaxLength(1024)] public string? GrantedScopes { get; set; }
    [MaxLength(128)] public string? TenantId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(45)] public string? LastIpAddress { get; set; }
    [MaxLength(512)] public string? LastUserAgent { get; set; }
    public int RefreshCount { get; set; } = 0;
}
