using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

[Authorize]
public abstract class BaseOrgController : Controller
{
    protected readonly AppDbContext Db;
    protected readonly UserManager<AppUser> UserMgr;

    protected BaseOrgController(AppDbContext db, UserManager<AppUser> um)
    {
        Db = db;
        UserMgr = um;
    }

    protected async Task<int?> GetOrgId()
    {
        var user = await UserMgr.GetUserAsync(User);
        return user?.OrganizationId;
    }

    protected async Task<AppUser?> GetCurrentUser()
    {
        return await UserMgr.GetUserAsync(User);
    }

    protected IQueryable<Team> OrgTeams(int orgId) =>
        Db.Teams.Where(t => t.OrganizationId == orgId);

    protected async Task<HashSet<int>> OrgMemberIds(int orgId) =>
        (await OrgTeams(orgId).Include(t => t.Members)
            .SelectMany(t => t.Members).Select(m => m.Id).ToListAsync()).ToHashSet();

    protected async Task<List<string>> OrgProjects(int orgId) =>
        await OrgTeams(orgId).Select(t => t.Project).Distinct().OrderBy(p => p).ToListAsync();

    protected bool CanEdit(AppUser user) =>
        user.OrgRole == AppUser.Roles.Owner || user.OrgRole == AppUser.Roles.Manager;
}
