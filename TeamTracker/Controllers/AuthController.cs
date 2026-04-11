using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

public class AuthController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AppDbContext _db;

    public AuthController(UserManager<AppUser> um, SignInManager<AppUser> sm, AppDbContext db)
    {
        _userManager = um;
        _signInManager = sm;
        _db = db;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var callbackUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", callbackUrl);
        return Challenge(properties, "Google");
    }

    [HttpGet]
    public IActionResult GoogleLoginDirect(string? returnUrl = null)
    {
        var callbackUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", callbackUrl);
        return Challenge(properties, "Google");
    }

    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null) return RedirectToAction(nameof(Login));

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var avatar = info.Principal.FindFirstValue("picture");

        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Login));

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                DisplayName = name ?? email,
                AvatarUrl = avatar,
                EmailConfirmed = true,
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return RedirectToAction(nameof(Login));
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: true);

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    // ── Setup Organization (first-time after login) ──
    public async Task<IActionResult> Setup()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        if (user.OrganizationId != null) return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(string orgName)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        if (user.OrganizationId != null) return RedirectToAction("Index", "Home");

        if (string.IsNullOrWhiteSpace(orgName))
        {
            ModelState.AddModelError("", "Organization name is required.");
            return View();
        }

        var org = new Organization
        {
            Name = orgName.Trim(),
            OwnerId = user.Id,
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        user.OrganizationId = org.Id;
        user.OrgRole = AppUser.Roles.Owner;
        await _userManager.UpdateAsync(user);

        return RedirectToAction("Index", "Home");
    }

    // ── Join via invite link ──
    public async Task<IActionResult> Join(string code)
    {
        var invite = await _db.InviteLinks
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Code == code);

        if (invite == null || !invite.IsValid)
            return View("InvalidInvite");

        ViewBag.Invite = invite;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvite(string code)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login), new { returnUrl = $"/Auth/Join/{code}" });

        var invite = await _db.InviteLinks.Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Code == code);

        if (invite == null || !invite.IsValid)
            return View("InvalidInvite");

        user.OrganizationId = invite.OrganizationId;
        user.OrgRole = invite.Role;
        await _userManager.UpdateAsync(user);

        invite.TimesUsed++;
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Welcome to {invite.Organization.Name}!";
        return RedirectToAction("Index", "Home");
    }
}
