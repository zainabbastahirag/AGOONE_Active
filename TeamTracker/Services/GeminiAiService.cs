using Google.GenAI;
using TeamTracker.Models;

namespace TeamTracker.Services;

public class GeminiAiService
{
    private readonly string? _apiKey;
    private readonly bool _isConfigured;

    public GeminiAiService(IConfiguration config)
    {
        _apiKey = config["Gemini:ApiKey"];
        _isConfigured = !string.IsNullOrEmpty(_apiKey)
            && _apiKey != "YOUR_GEMINI_API_KEY"
            && _apiKey.Length > 10;
    }

    public bool IsAvailable => _isConfigured;

    public async Task<string> GenerateFeedback(MonthlyKpi kpi)
    {
        if (!_isConfigured) return "";

        var prompt = $@"You are a Tech Manager writing monthly performance feedback for a team member.
Write 3-4 sentences of constructive, professional feedback.

Member: {kpi.Member.Name}, Role: {kpi.Member.Role}, Team: {kpi.Member.Team.Name}, Project: {kpi.Member.Team.Project}
KPIs: {kpi.TasksCompleted}/{kpi.TasksAssigned} tasks ({kpi.CompletionPct}%), On-Time: {kpi.OnTimeDeliveryPct}%, Bugs: {kpi.BugsFoundInWork}, Quality: {kpi.QualityScore}/5, Hours: {kpi.TotalHoursWorked}, Overtime: {kpi.ExtraHours}, Grade: {kpi.Grade}
{(string.IsNullOrEmpty(kpi.Achievements) ? "" : $"Achievements: {kpi.Achievements}")}
{(string.IsNullOrEmpty(kpi.ErrorsLog) ? "" : $"Issues: {kpi.ErrorsLog}")}

Write direct feedback with actual numbers. Encouraging but honest. Under 100 words. No bullet points.";

        return await CallGemini(prompt);
    }

    public async Task<string> GenerateTeamSummary(List<MonthlyKpi> kpis, List<Team> teams)
    {
        if (!_isConfigured || !kpis.Any()) return "";

        var active = kpis.Where(k => k.TasksAssigned > 0).ToList();
        var top = active.OrderByDescending(k => k.CompletionPct).Take(3).Select(k => k.Member.Name);
        var weak = active.Where(k => k.CompletionPct < 60).Select(k => k.Member.Name);

        var prompt = $@"Write a 4-5 sentence executive summary for leadership.
{active.Count} members, {teams.Count} teams, {teams.Select(t=>t.Project).Distinct().Count()} projects.
Tasks: {active.Sum(k=>k.TasksCompleted)}/{active.Sum(k=>k.TasksAssigned)} ({active.Average(k=>k.CompletionPct):N0}% avg).
Bugs: {active.Sum(k=>k.BugsFoundInWork)}, Quality: {active.Where(k=>k.QualityScore>0).Select(k=>(double)k.QualityScore).DefaultIfEmpty(0).Average():N1}/5.
Hours: {kpis.Sum(k=>k.TotalHoursWorked):N0}, Overtime: {kpis.Sum(k=>k.ExtraHours):N0}.
Top: {string.Join(", ", top)}. {(weak.Any() ? $"Concern: {string.Join(", ", weak)}" : "No underperformers.")}
Write for VP/CTO. Under 120 words. No bullets.";

        return await CallGemini(prompt);
    }

    public async Task<string> GenerateRecommendations(List<MonthlyKpi> kpis)
    {
        if (!_isConfigured || !kpis.Any()) return "";

        var bugs = kpis.Where(k => k.BugRatio > 0.2).Select(k => k.Member.Name).ToList();
        var burnout = kpis.Where(k => k.ExtraHours > 15).Select(k => $"{k.Member.Name}({k.ExtraHours}h)").ToList();
        var low = kpis.Where(k => k.TasksAssigned > 0 && k.CompletionPct < 60).Select(k => k.Member.Name).ToList();

        var prompt = $@"Generate 3-5 actionable recommendations for a Tech Manager:
{(bugs.Any() ? $"High bug ratio: {string.Join(", ", bugs)}" : "Bug ratios healthy")}
{(burnout.Any() ? $"Burnout risk: {string.Join(", ", burnout)}" : "Overtime normal")}
{(low.Any() ? $"Low completion: {string.Join(", ", low)}" : "Completion good")}
{kpis.Count(k=>k.CodeReviewsDone==0)} of {kpis.Count} did zero code reviews.

Each recommendation: one sentence, starts with action verb, mention names. Number them. Under 150 words.";

        return await CallGemini(prompt);
    }

    public async Task<string> GenerateDailySummary(List<DailyLog> logs, DateTime date)
    {
        if (!_isConfigured || !logs.Any()) return "";

        var summary = string.Join("\n", logs.Take(15).Select(l =>
            $"- {l.Member.Name}: {l.TaskDescription} [{l.Status}] {l.HoursWorked}h" +
            (string.IsNullOrEmpty(l.Blockers) ? "" : $" BLOCKED:{l.Blockers}")));

        var prompt = $@"Write a 3-4 sentence daily standup summary for {date:dddd dd MMMM}.
{logs.Count(l=>l.Status=="Completed")} completed, {logs.Count(l=>l.Status=="In Progress")} in progress, {logs.Count(l=>l.Status=="Blocked")} blocked. {logs.Sum(l=>l.HoursWorked)} hours.
{summary}
Under 80 words. No bullets.";

        return await CallGemini(prompt);
    }

    private async Task<string> CallGemini(string prompt)
    {
        try
        {
            var client = new Client(apiKey: _apiKey);
            var response = await client.Models.GenerateContentAsync(
                model: "gemini-2.5-flash-lite",
                contents: prompt
            );
            return response?.Text?.Trim() ?? "";
        }
        catch (Exception ex)
        {
            return $"[AI error: {ex.Message}]";
        }
    }
}
