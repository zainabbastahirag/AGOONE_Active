using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class InviteLink
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = Guid.NewGuid().ToString("N")[..12];

    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [MaxLength(20)]
    public string Role { get; set; } = AppUser.Roles.Viewer;

    public bool IsActive { get; set; } = true;

    public int MaxUses { get; set; } = 0;
    public int TimesUsed { get; set; } = 0;

    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsValid => IsActive
        && (MaxUses == 0 || TimesUsed < MaxUses)
        && (!ExpiresAt.HasValue || ExpiresAt > DateTime.UtcNow);
}
