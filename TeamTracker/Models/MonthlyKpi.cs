using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class MonthlyKpi
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public int Year { get; set; } = DateTime.Now.Year;
    public int Month { get; set; } = DateTime.Now.Month;

    public int TasksAssigned { get; set; }
    public int TasksCompleted { get; set; }
    public int BugsFoundInWork { get; set; }
    public int BugsFixed { get; set; }
    public int CodeReviewsDone { get; set; }

    [Range(0, 100)]
    public int OnTimeDeliveryPct { get; set; }

    [Range(0, 5)]
    public int QualityScore { get; set; }

    public double TotalHoursWorked { get; set; }
    public double ExtraHours { get; set; }

    [MaxLength(2000)]
    public string Achievements { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ErrorsLog { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string TechLeadFeedback { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ManagerFeedback { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ProgressNotes { get; set; } = string.Empty;

    // Computed properties
    public double CompletionPct => TasksAssigned > 0
        ? Math.Round((double)TasksCompleted / TasksAssigned * 100, 1) : 0;

    public double BugRatio => TasksCompleted > 0
        ? Math.Round((double)BugsFoundInWork / TasksCompleted, 2) : 0;

    public string Grade
    {
        get
        {
            if (TasksAssigned == 0) return "-";
            double score = (CompletionPct / 100 * 30)
                         + (OnTimeDeliveryPct / 100.0 * 30)
                         + (QualityScore / 5.0 * 25)
                         + ((1 - Math.Min(BugRatio, 1)) * 15);
            return score >= 85 ? "A" : score >= 70 ? "B" : score >= 55 ? "C" : score >= 40 ? "D" : "F";
        }
    }

    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
}
