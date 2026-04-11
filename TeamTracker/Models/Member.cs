using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class Member
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ProjectsAssigned { get; set; } = string.Empty;

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public ICollection<DailyLog> DailyLogs { get; set; } = new List<DailyLog>();
    public ICollection<MonthlyKpi> MonthlyKpis { get; set; } = new List<MonthlyKpi>();
}
