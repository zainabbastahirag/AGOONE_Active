using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

public class TeamsController : BaseOrgController
{
    public TeamsController(AppDbContext db, UserManager<AppUser> um) : base(db, um) { }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string Name, string Project, string TechLead)
    {
        var user = await GetCurrentUser();
        if (user?.OrganizationId == null || !CanEdit(user)) return Forbid();

        var team = new Team
        {
            Name = Name ?? "",
            Project = Project ?? "",
            TechLead = TechLead ?? "",
            OrganizationId = user.OrganizationId.Value
        };

        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        TempData["Message"] = $"Team \"{team.Name}\" created!";
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var orgId = await GetOrgId();
        var team = await Db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
        if (team == null) return NotFound();
        return View(team);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string Name, string Project, string TechLead)
    {
        var user = await GetCurrentUser();
        if (user?.OrganizationId == null || !CanEdit(user)) return Forbid();

        var team = await Db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == user.OrganizationId);
        if (team == null) return NotFound();

        team.Name = Name ?? team.Name;
        team.Project = Project ?? team.Project;
        team.TechLead = TechLead ?? team.TechLead;

        await Db.SaveChangesAsync();
        TempData["Message"] = $"Team \"{team.Name}\" updated!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();
        var team = await Db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == user!.OrganizationId);
        if (team != null) { Db.Teams.Remove(team); await Db.SaveChangesAsync(); TempData["Message"] = "Team deleted."; }
        return RedirectToAction("Index", "Home");
    }
}
