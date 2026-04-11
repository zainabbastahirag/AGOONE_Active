using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

public class MembersController : BaseOrgController
{
    public MembersController(AppDbContext db, UserManager<AppUser> um) : base(db, um) { }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string Name, string Role, int TeamId, string? ProjectsAssigned)
    {
        var user = await GetCurrentUser();
        if (user?.OrganizationId == null || !CanEdit(user)) return Forbid();

        // Verify team belongs to this org
        var team = await Db.Teams.FirstOrDefaultAsync(t => t.Id == TeamId && t.OrganizationId == user.OrganizationId);
        if (team == null) { TempData["Message"] = "Invalid team."; return RedirectToAction("Index", "Home"); }

        var member = new Member
        {
            Name = Name ?? "",
            Role = Role ?? "Developer",
            TeamId = TeamId,
            ProjectsAssigned = ProjectsAssigned ?? team.Project
        };

        Db.Members.Add(member);
        await Db.SaveChangesAsync();

        TempData["Message"] = $"{member.Name} added to {team.Name}!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();
        var member = await Db.Members.Include(m => m.Team).FirstOrDefaultAsync(m => m.Id == id);
        if (member != null && member.Team.OrganizationId == user!.OrganizationId)
        {
            Db.Members.Remove(member);
            await Db.SaveChangesAsync();
            TempData["Message"] = $"{member.Name} removed.";
        }
        return RedirectToAction("Index", "Home");
    }
}
