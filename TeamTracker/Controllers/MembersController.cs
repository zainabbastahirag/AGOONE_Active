using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

public class MembersController : BaseOrgController
{
    public MembersController(AppDbContext db, UserManager<AppUser> um) : base(db, um) { }

    public async Task<IActionResult> Index()
    {
        var orgId = await GetOrgId();
        if (orgId == null) return RedirectToAction("Setup", "Auth");

        var members = await Db.Members.Include(m => m.Team)
            .Where(m => m.Team.OrganizationId == orgId)
            .OrderBy(m => m.Team.Name).ThenBy(m => m.Name).ToListAsync();
        return View(members);
    }

    public async Task<IActionResult> Create()
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();
        ViewBag.Teams = new SelectList(
            await OrgTeams(orgId.Value).OrderBy(t => t.Name).ToListAsync(), "Id", "Name");
        return View(new Member());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Member member)
    {
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Teams = new SelectList(
                await OrgTeams(user!.OrganizationId!.Value).OrderBy(t => t.Name).ToListAsync(), "Id", "Name");
            return View(member);
        }

        Db.Members.Add(member);
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();
        var member = await Db.Members.FindAsync(id);
        if (member == null) return NotFound();
        ViewBag.Teams = new SelectList(
            await OrgTeams(orgId.Value).OrderBy(t => t.Name).ToListAsync(), "Id", "Name", member.TeamId);
        return View(member);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Member member)
    {
        if (id != member.Id) return NotFound();
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Teams = new SelectList(
                await OrgTeams(user!.OrganizationId!.Value).OrderBy(t => t.Name).ToListAsync(), "Id", "Name", member.TeamId);
            return View(member);
        }

        Db.Update(member);
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();
        var member = await Db.Members.FindAsync(id);
        if (member != null) { Db.Members.Remove(member); await Db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
