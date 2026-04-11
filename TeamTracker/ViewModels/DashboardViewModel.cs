using TeamTracker.Models;

namespace TeamTracker.ViewModels;

public class DashboardViewModel
{
    public int TotalTeams { get; set; }
    public int TotalMembers { get; set; }
    public int TotalProjects { get; set; }
    public int TotalTasksAssigned { get; set; }
    public int TotalTasksCompleted { get; set; }
    public double OverallCompletionPct { get; set; }
    public int TotalBugsFound { get; set; }
    public int TotalBugsFixed { get; set; }
    public double TotalHoursWorked { get; set; }
    public double TotalExtraHours { get; set; }
    public double AvgQuality { get; set; }
    public int SelectedMonth { get; set; }
    public int SelectedYear { get; set; }

    public List<Team> Teams { get; set; } = new();
    public List<MonthlyKpi> Kpis { get; set; } = new();
    public List<DailyLog> RecentLogs { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public Dictionary<string, double> ProjectHours { get; set; } = new();
}
