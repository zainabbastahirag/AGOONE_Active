using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class Team
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Project { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string TechLead { get; set; } = string.Empty;

    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public ICollection<Member> Members { get; set; } = new List<Member>();
}
