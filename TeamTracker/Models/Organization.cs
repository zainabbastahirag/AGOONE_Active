using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class Organization
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<InviteLink> InviteLinks { get; set; } = new List<InviteLink>();
}
