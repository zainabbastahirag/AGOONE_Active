using TeamTracker.Models;

namespace TeamTracker.Services;

/// <summary>
/// Free AI engine — zero API cost, runs entirely on-server.
/// Analyzes KPI data and daily logs to produce smart insights,
/// risk alerts, performance summaries, feedback drafts, and recommendations.
/// </summary>
public class AiInsightEngine
{
    // ─── Data Models ─────────────────────────────────────────────

    public class AiReport
    {
        public int TeamHealthScore { get; set; }
        public string TeamHealthLabel { get; set; } = "";
        public string TeamHealthColor { get; set; } = "";
        public List<AiAlert> Alerts { get; set; } = new();
        public List<MemberInsight> MemberInsights { get; set; } = new();
        public List<string> TopAchievers { get; set; } = new();
        public List<string> NeedAttention { get; set; } = new();
        public WorkloadAnalysis Workload { get; set; } = new();
        public List<AiRecommendation> Recommendations { get; set; } = new();
        public string ExecutiveSummary { get; set; } = "";
    }

    public class AiAlert
    {
        public string Level { get; set; } = "";    // critical, warning, info, success
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    public class MemberInsight
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = "";
        public string Team { get; set; } = "";
        public string Role { get; set; } = "";
        public string PerformanceSummary { get; set; } = "";
        public string FeedbackDraft { get; set; } = "";
        public string RiskLevel { get; set; } = "";   // low, medium, high
        public string RiskReason { get; set; } = "";
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public string Emoji { get; set; } = "";
    }

    public class WorkloadAnalysis
    {
        public List<(string Name, double Hours, string Level)> Distribution { get; set; } = new();
        public double AverageHours { get; set; }
        public double MaxHours { get; set; }
        public string MostOverloaded { get; set; } = "";
        public string LeastLoaded { get; set; } = "";
        public bool IsBalanced { get; set; }
    }

    public class AiRecommendation
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Priority { get; set; } = "";  // high, medium, low
    }

    // ─── Main Analysis ──────────────────────────────────────────

    public AiReport Analyze(List<MonthlyKpi> kpis, List<DailyLog> logs, List<Team> teams)
    {
        var report = new AiReport();

        if (!kpis.Any())
        {
            report.TeamHealthScore = 0;
            report.TeamHealthLabel = "No Data";
            report.TeamHealthColor = "gray";
            report.ExecutiveSummary = "No KPI data available for this period. Generate KPI records and fill in data to get AI-powered insights.";
            return report;
        }

        // ── Team Health Score (0-100) ──
        report.TeamHealthScore = CalculateTeamHealth(kpis);
        (report.TeamHealthLabel, report.TeamHealthColor) = report.TeamHealthScore switch
        {
            >= 85 => ("Excellent", "#059669"),
            >= 70 => ("Good", "#2563eb"),
            >= 55 => ("Fair", "#d97706"),
            >= 40 => ("Needs Attention", "#ea580c"),
            _ => ("Critical", "#dc2626"),
        };

        // ── Alerts ──
        report.Alerts = GenerateAlerts(kpis, logs);

        // ── Member Insights ──
        report.MemberInsights = kpis.Select(k => AnalyzeMember(k)).ToList();

        // ── Top Achievers & Need Attention ──
        report.TopAchievers = kpis
            .Where(k => k.TasksAssigned > 0)
            .OrderByDescending(k => ScoreMember(k))
            .Take(3)
            .Select(k => k.Member.Name)
            .ToList();

        report.NeedAttention = kpis
            .Where(k => k.TasksAssigned > 0 && ScoreMember(k) < 50)
            .OrderBy(k => ScoreMember(k))
            .Take(3)
            .Select(k => k.Member.Name)
            .ToList();

        // ── Workload ──
        report.Workload = AnalyzeWorkload(kpis);

        // ── Recommendations ──
        report.Recommendations = GenerateRecommendations(kpis, logs, teams);

        // ── Executive Summary ──
        report.ExecutiveSummary = GenerateExecutiveSummary(report, kpis);

        return report;
    }

    // ─── Team Health Score ───────────────────────────────────────

    private int CalculateTeamHealth(List<MonthlyKpi> kpis)
    {
        var active = kpis.Where(k => k.TasksAssigned > 0).ToList();
        if (!active.Any()) return 0;

        double avgCompletion = active.Average(k => k.CompletionPct);
        double avgOnTime = active.Average(k => k.OnTimeDeliveryPct);
        double avgQuality = active.Where(k => k.QualityScore > 0).Select(k => (double)k.QualityScore).DefaultIfEmpty(0).Average();
        double avgBugRatio = active.Average(k => k.BugRatio);

        double score = (avgCompletion * 0.30)
                     + (avgOnTime * 0.25)
                     + (avgQuality / 5 * 100 * 0.25)
                     + ((1 - Math.Min(avgBugRatio, 1)) * 100 * 0.20);

        return (int)Math.Clamp(Math.Round(score), 0, 100);
    }

    // ─── Alerts ─────────────────────────────────────────────────

    private List<AiAlert> GenerateAlerts(List<MonthlyKpi> kpis, List<DailyLog> logs)
    {
        var alerts = new List<AiAlert>();
        var active = kpis.Where(k => k.TasksAssigned > 0).ToList();

        // Burnout detection
        var burnoutRisk = kpis.Where(k => k.ExtraHours > 20).ToList();
        if (burnoutRisk.Any())
            alerts.Add(new AiAlert
            {
                Level = "critical", Icon = "bi-fire",
                Title = "Burnout Risk Detected",
                Detail = $"{string.Join(", ", burnoutRisk.Select(k => k.Member.Name))} logged {burnoutRisk.Sum(k => k.ExtraHours):N0} extra hours. Consider redistributing workload."
            });

        // Low completion rate
        var lowCompletion = active.Where(k => k.CompletionPct < 50).ToList();
        if (lowCompletion.Any())
            alerts.Add(new AiAlert
            {
                Level = "warning", Icon = "bi-exclamation-triangle",
                Title = "Low Completion Rate",
                Detail = $"{string.Join(", ", lowCompletion.Select(k => k.Member.Name))} completed less than 50% of assigned tasks. Check for blockers."
            });

        // High bug ratio
        var buggy = active.Where(k => k.BugRatio > 0.3).ToList();
        if (buggy.Any())
            alerts.Add(new AiAlert
            {
                Level = "warning", Icon = "bi-bug",
                Title = "Quality Concern",
                Detail = $"{string.Join(", ", buggy.Select(k => k.Member.Name))} have a bug ratio above 30%. Consider code review improvements."
            });

        // Blocked tasks in daily log
        var blockedCount = logs.Count(l => l.Status == "Blocked");
        if (blockedCount > 3)
            alerts.Add(new AiAlert
            {
                Level = "warning", Icon = "bi-shield-exclamation",
                Title = $"{blockedCount} Blocked Tasks",
                Detail = "Multiple tasks are blocked this month. Review blockers in the daily log to unblock your team."
            });

        // Uneven workload
        if (active.Count >= 3)
        {
            var hours = active.Select(k => k.TotalHoursWorked).ToList();
            var max = hours.Max();
            var min = hours.Where(h => h > 0).DefaultIfEmpty(0).Min();
            if (max > 0 && min > 0 && max / min > 2.5)
                alerts.Add(new AiAlert
                {
                    Level = "info", Icon = "bi-bar-chart-steps",
                    Title = "Workload Imbalance",
                    Detail = $"Highest workload is {max:N0}h vs lowest {min:N0}h ({max / min:N1}x difference). Consider rebalancing."
                });
        }

        // Positive alerts
        var highPerformers = active.Where(k => k.CompletionPct >= 90 && k.OnTimeDeliveryPct >= 90).ToList();
        if (highPerformers.Any())
            alerts.Add(new AiAlert
            {
                Level = "success", Icon = "bi-star-fill",
                Title = "Outstanding Performance",
                Detail = $"{string.Join(", ", highPerformers.Select(k => k.Member.Name))} achieved 90%+ completion AND on-time delivery. Great work!"
            });

        // No daily logs
        if (!logs.Any())
            alerts.Add(new AiAlert
            {
                Level = "info", Icon = "bi-journal-x",
                Title = "No Daily Logs",
                Detail = "No daily log entries found. Encourage your team to log their daily work for better tracking."
            });

        return alerts.OrderBy(a => a.Level switch { "critical" => 0, "warning" => 1, "info" => 2, _ => 3 }).ToList();
    }

    // ─── Member Analysis ────────────────────────────────────────

    private double ScoreMember(MonthlyKpi k)
    {
        if (k.TasksAssigned == 0) return 0;
        return (k.CompletionPct * 0.30) + (k.OnTimeDeliveryPct * 0.30)
             + (k.QualityScore / 5.0 * 100 * 0.25) + ((1 - Math.Min(k.BugRatio, 1)) * 100 * 0.15);
    }

    private MemberInsight AnalyzeMember(MonthlyKpi k)
    {
        var insight = new MemberInsight
        {
            MemberId = k.MemberId,
            Name = k.Member.Name,
            Team = k.Member.Team.Name,
            Role = k.Member.Role,
        };

        if (k.TasksAssigned == 0)
        {
            insight.PerformanceSummary = "No tasks assigned this period.";
            insight.FeedbackDraft = "No activity to evaluate. Ensure tasks are being assigned and tracked.";
            insight.RiskLevel = "medium";
            insight.RiskReason = "No tasks assigned";
            insight.Emoji = "⏸️";
            return insight;
        }

        var score = ScoreMember(k);

        // Risk level
        if (score >= 75) { insight.RiskLevel = "low"; insight.RiskReason = "On track"; }
        else if (score >= 50) { insight.RiskLevel = "medium"; insight.RiskReason = "Some areas need improvement"; }
        else { insight.RiskLevel = "high"; insight.RiskReason = "Performance below expectations"; }

        // Emoji
        insight.Emoji = score >= 85 ? "🌟" : score >= 70 ? "👍" : score >= 50 ? "⚡" : score >= 30 ? "⚠️" : "🚨";

        // Strengths
        if (k.CompletionPct >= 80) insight.Strengths.Add($"Strong completion rate ({k.CompletionPct}%)");
        if (k.OnTimeDeliveryPct >= 85) insight.Strengths.Add($"Excellent time management ({k.OnTimeDeliveryPct}% on-time)");
        if (k.QualityScore >= 4) insight.Strengths.Add($"High quality work (rated {k.QualityScore}/5)");
        if (k.BugRatio == 0 && k.TasksCompleted > 0) insight.Strengths.Add("Zero bugs in delivered work");
        if (k.CodeReviewsDone >= 5) insight.Strengths.Add($"Active code reviewer ({k.CodeReviewsDone} reviews)");
        if (k.ExtraHours > 0 && k.CompletionPct >= 80) insight.Strengths.Add("Goes above and beyond when needed");

        // Improvements
        if (k.CompletionPct < 60) insight.Improvements.Add($"Task completion needs improvement ({k.CompletionPct}%)");
        if (k.OnTimeDeliveryPct < 60) insight.Improvements.Add($"On-time delivery is low ({k.OnTimeDeliveryPct}%)");
        if (k.QualityScore > 0 && k.QualityScore <= 2) insight.Improvements.Add($"Quality score below average ({k.QualityScore}/5)");
        if (k.BugRatio > 0.3) insight.Improvements.Add($"High bug ratio ({k.BugRatio:N2}) — needs more testing");
        if (k.CodeReviewsDone == 0 && k.Member.Role.Contains("Lead")) insight.Improvements.Add("No code reviews done (expected for lead role)");

        // Performance summary
        insight.PerformanceSummary = GeneratePerformanceSummary(k, score);

        // Feedback draft
        insight.FeedbackDraft = GenerateFeedbackDraft(k, score, insight.Strengths, insight.Improvements);

        return insight;
    }

    private string GeneratePerformanceSummary(MonthlyKpi k, double score)
    {
        var name = k.Member.Name.Split(' ')[0];
        var level = score >= 85 ? "exceptional" : score >= 70 ? "solid" : score >= 50 ? "moderate" : "below expectations";

        var summary = $"{name} delivered {level} performance this month. ";

        if (k.TasksCompleted > 0)
            summary += $"Completed {k.TasksCompleted} of {k.TasksAssigned} tasks ({k.CompletionPct}%). ";

        if (k.OnTimeDeliveryPct > 0)
            summary += $"On-time delivery was {k.OnTimeDeliveryPct}%. ";

        if (k.BugsFoundInWork > 0)
            summary += $"{k.BugsFoundInWork} bug{(k.BugsFoundInWork > 1 ? "s" : "")} found in delivered work. ";
        else if (k.TasksCompleted > 0)
            summary += "Maintained clean delivery with zero bugs. ";

        if (k.ExtraHours > 10)
            summary += $"Logged {k.ExtraHours:N0} overtime hours — monitor for burnout. ";

        return summary.Trim();
    }

    private string GenerateFeedbackDraft(MonthlyKpi k, double score, List<string> strengths, List<string> improvements)
    {
        var name = k.Member.Name.Split(' ')[0];
        var parts = new List<string>();

        if (score >= 80)
            parts.Add($"{name} has shown excellent performance this month and is a valuable contributor to the team.");
        else if (score >= 60)
            parts.Add($"{name} has delivered reasonable work this month with room for growth in some areas.");
        else if (score >= 40)
            parts.Add($"{name}'s performance this month has fallen below expectations. A one-on-one discussion is recommended.");
        else
            parts.Add($"{name} needs immediate attention and support. Performance is significantly below target.");

        if (strengths.Any())
            parts.Add("Key strengths: " + string.Join("; ", strengths.Take(2)) + ".");

        if (improvements.Any())
            parts.Add("Areas to work on: " + string.Join("; ", improvements.Take(2)) + ".");

        if (k.Member.Role.Contains("Lead") && k.CodeReviewsDone > 0)
            parts.Add($"Leadership contribution noted through {k.CodeReviewsDone} code reviews.");

        return string.Join(" ", parts);
    }

    // ─── Workload Analysis ──────────────────────────────────────

    private WorkloadAnalysis AnalyzeWorkload(List<MonthlyKpi> kpis)
    {
        var w = new WorkloadAnalysis();
        var active = kpis.Where(k => k.TotalHoursWorked > 0).ToList();

        if (!active.Any()) return w;

        w.Distribution = active.Select(k =>
        {
            var level = k.TotalHoursWorked > 180 ? "overloaded"
                      : k.TotalHoursWorked > 140 ? "heavy"
                      : k.TotalHoursWorked > 80 ? "normal"
                      : "light";
            return (k.Member.Name, k.TotalHoursWorked, level);
        }).OrderByDescending(x => x.TotalHoursWorked).ToList();

        w.AverageHours = active.Average(k => k.TotalHoursWorked);
        w.MaxHours = active.Max(k => k.TotalHoursWorked);
        w.MostOverloaded = active.OrderByDescending(k => k.TotalHoursWorked).First().Member.Name;
        w.LeastLoaded = active.OrderBy(k => k.TotalHoursWorked).First().Member.Name;

        var stdDev = Math.Sqrt(active.Average(k => Math.Pow(k.TotalHoursWorked - w.AverageHours, 2)));
        w.IsBalanced = stdDev < w.AverageHours * 0.3;

        return w;
    }

    // ─── Recommendations ────────────────────────────────────────

    private List<AiRecommendation> GenerateRecommendations(List<MonthlyKpi> kpis, List<DailyLog> logs, List<Team> teams)
    {
        var recs = new List<AiRecommendation>();
        var active = kpis.Where(k => k.TasksAssigned > 0).ToList();

        if (!active.Any())
        {
            recs.Add(new AiRecommendation { Icon = "bi-lightning", Title = "Get Started", Detail = "Generate KPI records and start tracking your team's performance to unlock AI insights.", Priority = "high" });
            return recs;
        }

        var avgCompletion = active.Average(k => k.CompletionPct);
        var avgBugRatio = active.Average(k => k.BugRatio);
        var totalExtra = kpis.Sum(k => k.ExtraHours);
        var noReviews = active.Count(k => k.CodeReviewsDone == 0);

        if (avgCompletion < 70)
            recs.Add(new AiRecommendation { Icon = "bi-bullseye", Title = "Improve Task Scoping", Detail = $"Average completion is {avgCompletion:N0}%. Tasks may be too large. Break them into smaller deliverables.", Priority = "high" });

        if (avgBugRatio > 0.2)
            recs.Add(new AiRecommendation { Icon = "bi-shield-check", Title = "Strengthen QA Process", Detail = $"Average bug ratio is {avgBugRatio:N2}. Consider adding peer testing or automated tests.", Priority = "high" });

        if (totalExtra > 50)
            recs.Add(new AiRecommendation { Icon = "bi-heart-pulse", Title = "Monitor Team Wellbeing", Detail = $"Team logged {totalExtra:N0} overtime hours. Check workload distribution and deadlines.", Priority = "medium" });

        if (noReviews > active.Count / 2)
            recs.Add(new AiRecommendation { Icon = "bi-code-slash", Title = "Increase Code Reviews", Detail = $"{noReviews} of {active.Count} members did zero code reviews. Code reviews improve quality and knowledge sharing.", Priority = "medium" });

        var lowQuality = active.Where(k => k.QualityScore > 0 && k.QualityScore <= 2).ToList();
        if (lowQuality.Any())
            recs.Add(new AiRecommendation { Icon = "bi-award", Title = "Quality Coaching Needed", Detail = $"{string.Join(", ", lowQuality.Select(k => k.Member.Name))} scored low on quality. Pair them with senior developers.", Priority = "high" });

        var noFeedback = kpis.Count(k => string.IsNullOrEmpty(k.TechLeadFeedback) && string.IsNullOrEmpty(k.ManagerFeedback));
        if (noFeedback > kpis.Count / 2)
            recs.Add(new AiRecommendation { Icon = "bi-chat-heart", Title = "Add Feedback", Detail = $"{noFeedback} members have no feedback yet. Regular feedback improves retention and performance.", Priority = "low" });

        var singleProject = teams.Where(t => t.Members.Count <= 1).ToList();
        if (singleProject.Any())
            recs.Add(new AiRecommendation { Icon = "bi-people", Title = "Strengthen Small Teams", Detail = $"{string.Join(", ", singleProject.Select(t => t.Name))} have only 1 member. Consider cross-training.", Priority = "low" });

        return recs.OrderBy(r => r.Priority switch { "high" => 0, "medium" => 1, _ => 2 }).ToList();
    }

    // ─── Executive Summary ──────────────────────────────────────

    private string GenerateExecutiveSummary(AiReport report, List<MonthlyKpi> kpis)
    {
        var active = kpis.Where(k => k.TasksAssigned > 0).ToList();
        if (!active.Any()) return "No performance data available for this period.";

        var totalAssigned = active.Sum(k => k.TasksAssigned);
        var totalCompleted = active.Sum(k => k.TasksCompleted);
        var avgCompletion = active.Average(k => k.CompletionPct);
        var totalBugs = active.Sum(k => k.BugsFoundInWork);
        var totalHours = kpis.Sum(k => k.TotalHoursWorked);

        var summary = $"This month, the team of {active.Count} members completed {totalCompleted} of {totalAssigned} tasks " +
                      $"({avgCompletion:N0}% completion rate) across {totalHours:N0} working hours. ";

        if (totalBugs == 0)
            summary += "Excellent quality — zero bugs found in delivered work. ";
        else
            summary += $"{totalBugs} bug{(totalBugs > 1 ? "s were" : " was")} identified in delivered work. ";

        summary += $"Overall team health is rated {report.TeamHealthLabel} ({report.TeamHealthScore}/100). ";

        if (report.TopAchievers.Any())
            summary += $"Top performers: {string.Join(", ", report.TopAchievers)}. ";

        var criticals = report.Alerts.Count(a => a.Level == "critical");
        var warnings = report.Alerts.Count(a => a.Level == "warning");
        if (criticals > 0)
            summary += $"⚠️ {criticals} critical alert{(criticals > 1 ? "s" : "")} require immediate attention. ";
        else if (warnings > 0)
            summary += $"{warnings} warning{(warnings > 1 ? "s" : "")} to review. ";
        else
            summary += "No critical issues detected. ";

        return summary.Trim();
    }
}
