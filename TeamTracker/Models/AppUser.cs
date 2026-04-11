using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class AppUser : IdentityUser
{
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? AvatarUrl { get; set; }

    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    [MaxLength(20)]
    public string OrgRole { get; set; } = "Viewer";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static class Roles
    {
        public const string Owner = "Owner";
        public const string Manager = "Manager";
        public const string Viewer = "Viewer";
    }
}
