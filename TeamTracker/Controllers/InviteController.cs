using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

[Authorize]
public class InviteController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public InviteController(AppDbContext db, UserManager<AppUser> um)
    {
        _db = db;
        _userManager = um;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.OrganizationId == null) return RedirectToAction("Setup", "Auth");
        if (user.OrgRole != AppUser.Roles.Owner && user.OrgRole != AppUser.Roles.Manager)
            return Forbid();

        var links = await _db.InviteLinks
            .Where(l => l.OrganizationId == user.OrganizationId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var members = await _db.Users
            .Where(u => u.OrganizationId == user.OrganizationId)
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        ViewBag.Members = members;
        ViewBag.OrgName = (await _db.Organizations.FindAsync(user.OrganizationId))?.Name;

        return View(links);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string role, int maxUses, int? expiryDays)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.OrganizationId == null) return Forbid();
        if (user.OrgRole != AppUser.Roles.Owner && user.OrgRole != AppUser.Roles.Manager)
            return Forbid();

        var link = new InviteLink
        {
            OrganizationId = user.OrganizationId.Value,
            Role = role,
            MaxUses = maxUses,
            ExpiresAt = expiryDays.HasValue ? DateTime.UtcNow.AddDays(expiryDays.Value) : null,
        };

        _db.InviteLinks.Add(link);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Invite link created!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var link = await _db.InviteLinks.FindAsync(id);
        if (link == null || link.OrganizationId != user?.OrganizationId) return NotFound();

        link.IsActive = false;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string userId, string role)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.OrgRole != AppUser.Roles.Owner) return Forbid();

        var target = await _userManager.FindByIdAsync(userId);
        if (target == null || target.OrganizationId != currentUser.OrganizationId) return NotFound();

        target.OrgRole = role;
        await _userManager.UpdateAsync(target);

        TempData["Message"] = $"{target.DisplayName}'s role updated to {role}.";
        return RedirectToAction(nameof(Index));
    }
}
