using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

public class KpiController : BaseOrgController
{
    public KpiController(AppDbContext db, UserManager<AppUser> um) : base(db, um) { }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return RedirectToAction("Setup", "Auth");

        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;

        var mIds = await OrgMemberIds(orgId.Value);

        var kpis = await Db.MonthlyKpis
            .Include(k => k.Member).ThenInclude(mb => mb.Team)
            .Where(k => k.Year == y && k.Month == m && mIds.Contains(k.MemberId))
            .OrderBy(k => k.Member.Team.Name).ThenBy(k => k.Member.Name)
            .ToListAsync();

        ViewBag.Month = m;
        ViewBag.Year = y;
        ViewBag.MonthName = new DateTime(y, m, 1).ToString("MMMM yyyy");

        return View(kpis);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var kpi = await Db.MonthlyKpis.Include(k => k.Member).ThenInclude(mb => mb.Team).FirstOrDefaultAsync(k => k.Id == id);
        if (kpi == null) return NotFound();
        return View(kpi);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MonthlyKpi kpi)
    {
        if (id != kpi.Id) return NotFound();
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        if (!ModelState.IsValid)
        {
            kpi.Member = (await Db.MonthlyKpis.Include(k => k.Member).ThenInclude(m => m.Team).FirstOrDefaultAsync(k => k.Id == id))?.Member!;
            return View(kpi);
        }

        Db.Update(kpi);
        await Db.SaveChangesAsync();
        TempData["Message"] = $"KPI updated for {kpi.Member?.Name ?? "member"}.";
        return RedirectToAction("Index", "Home", new { month = kpi.Month, year = kpi.Year });
    }

    public async Task<IActionResult> Generate(int? month, int? year)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();
        var user = await GetCurrentUser();
        if (!CanEdit(user!)) return Forbid();

        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;

        var members = await Db.Members.Include(mb => mb.Team)
            .Where(mb => mb.Team.OrganizationId == orgId).ToListAsync();

        var existingIds = await Db.MonthlyKpis
            .Where(k => k.Year == y && k.Month == m)
            .Select(k => k.MemberId).ToListAsync();

        int created = 0;
        foreach (var member in members)
        {
            if (!existingIds.Contains(member.Id))
            {
                Db.MonthlyKpis.Add(new MonthlyKpi { MemberId = member.Id, Year = y, Month = m });
                created++;
            }
        }
        if (created > 0) await Db.SaveChangesAsync();

        TempData["Message"] = $"Generated KPI records for {created} members for {new DateTime(y, m, 1):MMMM yyyy}.";
        return RedirectToAction("Index", "Home", new { month = m, year = y });
    }
}
