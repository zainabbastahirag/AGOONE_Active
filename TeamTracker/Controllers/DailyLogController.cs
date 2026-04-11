using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;
using TeamTracker.ViewModels;

namespace TeamTracker.Controllers;

public class DailyLogController : BaseOrgController
{
    public DailyLogController(AppDbContext db, UserManager<AppUser> um) : base(db, um) { }

    private async Task<SelectList> GetMembers(int orgId, int? selected = null)
    {
        var members = await Db.Members.Include(m => m.Team)
            .Where(m => m.Team.OrganizationId == orgId)
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, Display = m.Name + " (" + m.Team.Name + ")" })
            .ToListAsync();
        return new SelectList(members, "Id", "Display", selected);
    }

    private async Task<SelectList> GetProjects(int orgId, string? selected = null)
    {
        var projects = await OrgProjects(orgId);
        return new SelectList(projects.Select(p => new { Id = p, Name = p }), "Id", "Name", selected);
    }

    public async Task<IActionResult> Index(DateTime? date, int? memberId, string? project, string? status)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return RedirectToAction("Setup", "Auth");

        var mIds = await OrgMemberIds(orgId.Value);
        var q = Db.DailyLogs
            .Include(d => d.Member).ThenInclude(m => m.Team)
            .Where(d => mIds.Contains(d.MemberId))
            .AsQueryable();

        if (date.HasValue) q = q.Where(d => d.Date.Date == date.Value.Date);
        if (memberId.HasValue) q = q.Where(d => d.MemberId == memberId.Value);
        if (!string.IsNullOrEmpty(project)) q = q.Where(d => d.Project == project);
        if (!string.IsNullOrEmpty(status)) q = q.Where(d => d.Status == status);

        var vm = new DailyLogIndexViewModel
        {
            Logs = await q.OrderByDescending(d => d.Date).ThenBy(d => d.Member.Name).Take(200).ToListAsync(),
            FilterDate = date, FilterMemberId = memberId, FilterProject = project, FilterStatus = status,
            Members = await GetMembers(orgId.Value, memberId),
            Projects = await GetProjects(orgId.Value, project),
        };
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var orgId = await GetOrgId();
        if (orgId == null) return RedirectToAction("Setup", "Auth");

        return View(new DailyLogEditViewModel
        {
            Log = new DailyLog { Date = DateTime.Today },
            Members = await GetMembers(orgId.Value),
            Projects = await GetProjects(orgId.Value),
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DailyLogEditViewModel vm)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        if (!ModelState.IsValid)
        {
            vm.Members = await GetMembers(orgId.Value, vm.Log.MemberId);
            vm.Projects = await GetProjects(orgId.Value, vm.Log.Project);
            return View(vm);
        }

        Db.DailyLogs.Add(vm.Log);
        await Db.SaveChangesAsync();
        TempData["Message"] = "Daily log entry added!";
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();
        var log = await Db.DailyLogs.FindAsync(id);
        if (log == null) return NotFound();

        return View(new DailyLogEditViewModel
        {
            Log = log,
            Members = await GetMembers(orgId.Value, log.MemberId),
            Projects = await GetProjects(orgId.Value, log.Project),
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DailyLogEditViewModel vm)
    {
        if (id != vm.Log.Id) return NotFound();
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        if (!ModelState.IsValid)
        {
            vm.Members = await GetMembers(orgId.Value, vm.Log.MemberId);
            vm.Projects = await GetProjects(orgId.Value, vm.Log.Project);
            return View(vm);
        }

        Db.Update(vm.Log);
        await Db.SaveChangesAsync();
        TempData["Message"] = "Daily log updated!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();
        var log = await Db.DailyLogs.FindAsync(id);
        if (log != null) { Db.DailyLogs.Remove(log); await Db.SaveChangesAsync(); TempData["Message"] = "Entry deleted."; }
        return RedirectToAction("Index", "Home");
    }
}
