using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgOne.Infrastructure.Auth.Entities;

[Table("UserSessions")]
public class UserSession
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required] [MaxLength(128)]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [Required] [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(256)] public string? Email { get; set; }

    [Required] [MaxLength(50)]
    public string InitiatedByProduct { get; set; } = string.Empty;

    [MaxLength(512)] public string? ProductsAccessed { get; set; }

    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedUtc { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(45)] public string? IpAddress { get; set; }
    [MaxLength(512)] public string? UserAgent { get; set; }
}
