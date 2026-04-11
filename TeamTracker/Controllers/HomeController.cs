using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;
using TeamTracker.Services;
using TeamTracker.ViewModels;

namespace TeamTracker.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _um;

    public HomeController(AppDbContext db, UserManager<AppUser> um) { _db = db; _um = um; }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var user = await _um.GetUserAsync(User);
        if (user?.OrganizationId == null) return RedirectToAction("Setup", "Auth");
        var oid = user.OrganizationId.Value;

        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;

        var teams = await _db.Teams.Include(t => t.Members)
            .Where(t => t.OrganizationId == oid).OrderBy(t => t.Name).ToListAsync();

        var memberIds = teams.SelectMany(t => t.Members).Select(mb => mb.Id).ToHashSet();

        var kpis = await _db.MonthlyKpis
            .Include(k => k.Member).ThenInclude(mb => mb.Team)
            .Where(k => k.Year == y && k.Month == m && memberIds.Contains(k.MemberId))
            .OrderBy(k => k.Member.Team.Name).ThenBy(k => k.Member.Name)
            .ToListAsync();

        var recentLogs = await _db.DailyLogs
            .Include(d => d.Member).ThenInclude(mb => mb.Team)
            .Where(d => memberIds.Contains(d.MemberId))
            .OrderByDescending(d => d.Date).ThenBy(d => d.Member.Name)
            .Take(50).ToListAsync();

        var projects = teams.Select(t => t.Project).Distinct().ToList();

        var vm = new DashboardViewModel
        {
            TotalTeams = teams.Count,
            TotalMembers = teams.SelectMany(t => t.Members).Select(mb => mb.Name).Distinct().Count(),
            TotalProjects = projects.Count,
            TotalTasksAssigned = kpis.Sum(k => k.TasksAssigned),
            TotalTasksCompleted = kpis.Sum(k => k.TasksCompleted),
            TotalBugsFound = kpis.Sum(k => k.BugsFoundInWork),
            TotalBugsFixed = kpis.Sum(k => k.BugsFixed),
            TotalHoursWorked = kpis.Sum(k => k.TotalHoursWorked),
            TotalExtraHours = kpis.Sum(k => k.ExtraHours),
            SelectedMonth = m,
            SelectedYear = y,
            Teams = teams,
            Kpis = kpis,
            RecentLogs = recentLogs,
        };

        vm.OverallCompletionPct = vm.TotalTasksAssigned > 0
            ? Math.Round((double)vm.TotalTasksCompleted / vm.TotalTasksAssigned * 100, 1) : 0;

        var qScores = kpis.Where(k => k.QualityScore > 0).Select(k => k.QualityScore).ToList();
        vm.AvgQuality = qScores.Count > 0 ? Math.Round(qScores.Average(), 1) : 0;

        ViewBag.CurrentUser = user;
        ViewBag.CanEdit = user.OrgRole == AppUser.Roles.Owner || user.OrgRole == AppUser.Roles.Manager;
        ViewBag.OrgName = (await _db.Organizations.FindAsync(oid))?.Name ?? "";

        ViewBag.InviteLinks = await _db.InviteLinks
            .Where(l => l.OrganizationId == oid)
            .OrderByDescending(l => l.CreatedAt).ToListAsync();

        ViewBag.OrgMembers = await _db.Users
            .Where(u => u.OrganizationId == oid)
            .OrderBy(u => u.DisplayName).ToListAsync();

        // AI Insights
        var aiEngine = new AiInsightEngine();
        ViewBag.AiReport = aiEngine.Analyze(kpis, recentLogs, teams);

        return View(vm);
    }
}
