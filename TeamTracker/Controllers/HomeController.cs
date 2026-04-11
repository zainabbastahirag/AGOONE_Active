using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;
using TeamTracker.ViewModels;

namespace TeamTracker.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public HomeController(AppDbContext db, UserManager<AppUser> um)
    {
        _db = db;
        _userManager = um;
    }

    private async Task<(AppUser user, int orgId)?> GetUserOrg()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.OrganizationId == null) return null;
        return (user, user.OrganizationId.Value);
    }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var uo = await GetUserOrg();
        if (uo == null) return RedirectToAction("Setup", "Auth");
        var orgId = uo.Value.orgId;

        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;

        var teams = await _db.Teams.Include(t => t.Members)
            .Where(t => t.OrganizationId == orgId).ToListAsync();

        var memberIds = teams.SelectMany(t => t.Members).Select(mb => mb.Id).ToHashSet();

        var kpis = await _db.MonthlyKpis
            .Include(k => k.Member).ThenInclude(mb => mb.Team)
            .Where(k => k.Year == y && k.Month == m && memberIds.Contains(k.MemberId))
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

        ViewBag.UserName = uo.Value.user.DisplayName;
        ViewBag.AvatarUrl = uo.Value.user.AvatarUrl;
        ViewBag.OrgRole = uo.Value.user.OrgRole;

        return View(vm);
    }
}
