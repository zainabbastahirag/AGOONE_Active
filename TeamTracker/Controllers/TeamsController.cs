using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

public class TeamsController : BaseOrgController
{
    public TeamsController(AppDbContext db, UserManager<AppUser> um) : base(db, um) { }

    public async Task<IActionResult> Index()
    {
        var orgId = await GetOrgId();
        if (orgId == null) return RedirectToAction("Setup", "Auth");

        var teams = await OrgTeams(orgId.Value).Include(t => t.Members).OrderBy(t => t.Name).ToListAsync();
        return View(teams);
    }

    public IActionResult Create() => View(new Team());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Team team)
    {
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        team.OrganizationId = user!.OrganizationId!.Value;

        if (!ModelState.IsValid) return View(team);

        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var orgId = await GetOrgId();
        var team = await Db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
        if (team == null) return NotFound();
        return View(team);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Team team)
    {
        if (id != team.Id) return NotFound();
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        team.OrganizationId = user!.OrganizationId!.Value;

        if (!ModelState.IsValid) return View(team);

        Db.Update(team);
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();
        var team = await Db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == user!.OrganizationId);
        if (team != null) { Db.Teams.Remove(team); await Db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
