using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;

namespace TeamTracker.Controllers;

public class ExportController : BaseOrgController
{
    public ExportController(AppDbContext db, UserManager<AppUser> um) : base(db, um) { }

    public IActionResult Index() => View();

    public async Task<IActionResult> FullReport(int? month, int? year)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();

        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;
        var mIds = await OrgMemberIds(orgId.Value);
        var monthName = new DateTime(y, m, 1).ToString("MMMM");

        var logs = await Db.DailyLogs.Include(d => d.Member).ThenInclude(x => x.Team)
            .Where(d => d.Date.Month == m && d.Date.Year == y && mIds.Contains(d.MemberId))
            .OrderBy(d => d.Date).ThenBy(d => d.Member.Name).ToListAsync();

        var kpis = await Db.MonthlyKpis.Include(k => k.Member).ThenInclude(x => x.Team)
            .Where(k => k.Year == y && k.Month == m && mIds.Contains(k.MemberId))
            .OrderBy(k => k.Member.Team.Name).ThenBy(k => k.Member.Name).ToListAsync();

        var teams = await OrgTeams(orgId.Value).Include(t => t.Members).OrderBy(t => t.Name).ToListAsync();

        using var wb = new XLWorkbook();

        BuildDailyLogSheet(wb.AddWorksheet("Daily Log"), logs);
        BuildKpiSheet(wb.AddWorksheet("KPI Scorecard"), kpis);
        BuildFeedbackSheet(wb.AddWorksheet("Feedback"), kpis);
        BuildRosterSheet(wb.AddWorksheet("Team Roster"), teams);
        BuildProjectSummarySheet(wb.AddWorksheet("Project Summary"), kpis, teams);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"KpiPulse_{monthName}_{y}.xlsx");
    }

    public async Task<IActionResult> DailyLog(DateTime? from, DateTime? to)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();

        var f = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var t = to ?? DateTime.Today;
        var mIds = await OrgMemberIds(orgId.Value);

        var logs = await Db.DailyLogs.Include(d => d.Member).ThenInclude(x => x.Team)
            .Where(d => d.Date >= f && d.Date <= t && mIds.Contains(d.MemberId))
            .OrderBy(d => d.Date).ThenBy(d => d.Member.Name).ToListAsync();

        using var wb = new XLWorkbook();
        BuildDailyLogSheet(wb.AddWorksheet("Daily Log"), logs);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DailyLog_{f:yyyyMMdd}_to_{t:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> MonthlyKpi(int? month, int? year)
    {
        var orgId = await GetOrgId();
        if (orgId == null) return Forbid();

        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;
        var mIds = await OrgMemberIds(orgId.Value);
        var monthName = new DateTime(y, m, 1).ToString("MMMM");

        var kpis = await Db.MonthlyKpis.Include(k => k.Member).ThenInclude(x => x.Team)
            .Where(k => k.Year == y && k.Month == m && mIds.Contains(k.MemberId))
            .OrderBy(k => k.Member.Team.Name).ThenBy(k => k.Member.Name).ToListAsync();

        var teams = await OrgTeams(orgId.Value).Include(t => t.Members).OrderBy(t => t.Name).ToListAsync();

        using var wb = new XLWorkbook();
        BuildKpiSheet(wb.AddWorksheet("KPI Scorecard"), kpis);
        BuildFeedbackSheet(wb.AddWorksheet("Feedback"), kpis);
        BuildRosterSheet(wb.AddWorksheet("Team Roster"), teams);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"KPI_{monthName}_{y}.xlsx");
    }

    // ── Sheet builders ──

    private static void Header(IXLWorksheet ws, string[] h, string color)
    {
        for (int i = 0; i < h.Length; i++)
        {
            ws.Cell(1, i + 1).Value = h[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(color);
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static void BuildDailyLogSheet(IXLWorksheet ws, List<DailyLog> logs)
    {
        Header(ws, new[] { "Date", "Day", "Name", "Team", "Project", "Task/Ticket", "Description", "Status", "Hours", "Extra", "Blockers", "Notes" }, "#1A365D");
        int r = 2;
        foreach (var l in logs)
        {
            ws.Cell(r, 1).Value = l.Date; ws.Cell(r, 1).Style.DateFormat.Format = "dd-MMM-yyyy";
            ws.Cell(r, 2).Value = l.Date.ToString("ddd");
            ws.Cell(r, 3).Value = l.Member.Name;
            ws.Cell(r, 4).Value = l.Member.Team.Name;
            ws.Cell(r, 5).Value = l.Project;
            ws.Cell(r, 6).Value = l.TaskTicket;
            ws.Cell(r, 7).Value = l.TaskDescription;
            ws.Cell(r, 8).Value = l.Status;
            ws.Cell(r, 9).Value = l.HoursWorked;
            ws.Cell(r, 10).Value = l.ExtraHours;
            ws.Cell(r, 11).Value = l.Blockers;
            ws.Cell(r, 12).Value = l.Notes;
            var c = l.Status switch { "Completed" => "#F0FFF4", "Blocked" => "#FFF5F5", "In Progress" => "#EBF4FF", "Carry Forward" => "#FFFFF0", _ => "" };
            if (!string.IsNullOrEmpty(c)) ws.Range(r, 1, r, 12).Style.Fill.BackgroundColor = XLColor.FromHtml(c);
            r++;
        }
        ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1);
    }

    private static void BuildKpiSheet(IXLWorksheet ws, List<MonthlyKpi> kpis)
    {
        Header(ws, new[] { "Team", "Project", "Name", "Role", "Assigned", "Completed", "Completion%", "Bugs In", "Fixed", "Ratio", "Reviews", "OnTime%", "Quality", "Hours", "Extra", "Grade" }, "#1A365D");
        int r = 2;
        foreach (var k in kpis)
        {
            ws.Cell(r, 1).Value = k.Member.Team.Name; ws.Cell(r, 2).Value = k.Member.Team.Project;
            ws.Cell(r, 3).Value = k.Member.Name; ws.Cell(r, 4).Value = k.Member.Role;
            ws.Cell(r, 5).Value = k.TasksAssigned; ws.Cell(r, 6).Value = k.TasksCompleted;
            ws.Cell(r, 7).Value = k.CompletionPct / 100; ws.Cell(r, 7).Style.NumberFormat.Format = "0%";
            ws.Cell(r, 8).Value = k.BugsFoundInWork; ws.Cell(r, 9).Value = k.BugsFixed;
            ws.Cell(r, 10).Value = k.BugRatio; ws.Cell(r, 11).Value = k.CodeReviewsDone;
            ws.Cell(r, 12).Value = k.OnTimeDeliveryPct; ws.Cell(r, 13).Value = k.QualityScore;
            ws.Cell(r, 14).Value = k.TotalHoursWorked; ws.Cell(r, 15).Value = k.ExtraHours;
            ws.Cell(r, 16).Value = k.Grade;
            var gc = k.Grade switch { "A" or "B" => "#F0FFF4", "C" => "#FFFFF0", "D" or "F" => "#FFF5F5", _ => "" };
            if (!string.IsNullOrEmpty(gc)) ws.Cell(r, 16).Style.Fill.BackgroundColor = XLColor.FromHtml(gc);
            r++;
        }
        ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1);
    }

    private static void BuildFeedbackSheet(IXLWorksheet ws, List<MonthlyKpi> kpis)
    {
        Header(ws, new[] { "Team", "Name", "Role", "Achievements", "Errors", "TL Feedback", "Mgr Feedback", "Progress" }, "#805AD5");
        int r = 2;
        foreach (var k in kpis)
        {
            ws.Cell(r, 1).Value = k.Member.Team.Name; ws.Cell(r, 2).Value = k.Member.Name;
            ws.Cell(r, 3).Value = k.Member.Role; ws.Cell(r, 4).Value = k.Achievements;
            ws.Cell(r, 5).Value = k.ErrorsLog; ws.Cell(r, 6).Value = k.TechLeadFeedback;
            ws.Cell(r, 7).Value = k.ManagerFeedback; ws.Cell(r, 8).Value = k.ProgressNotes;
            r++;
        }
        ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1);
    }

    private static void BuildRosterSheet(IXLWorksheet ws, List<Team> teams)
    {
        Header(ws, new[] { "Team", "Project", "Tech Lead", "Member", "Role", "Projects" }, "#38A169");
        int r = 2;
        foreach (var t in teams)
            foreach (var m in t.Members.OrderBy(x => x.Name))
            {
                ws.Cell(r, 1).Value = t.Name; ws.Cell(r, 2).Value = t.Project; ws.Cell(r, 3).Value = t.TechLead;
                ws.Cell(r, 4).Value = m.Name; ws.Cell(r, 5).Value = m.Role; ws.Cell(r, 6).Value = m.ProjectsAssigned;
                r++;
            }
        ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1);
    }

    private static void BuildProjectSummarySheet(IXLWorksheet ws, List<MonthlyKpi> kpis, List<Team> teams)
    {
        Header(ws, new[] { "Project", "Teams", "Members", "Hours", "Extra", "Assigned", "Completed" }, "#DD6B20");
        var projects = teams.Select(t => t.Project).Distinct().OrderBy(p => p).ToList();
        int r = 2;
        foreach (var proj in projects)
        {
            var pt = teams.Where(t => t.Project == proj).ToList();
            var ids = pt.SelectMany(t => t.Members).Select(m => m.Id).ToHashSet();
            var pk = kpis.Where(k => ids.Contains(k.MemberId)).ToList();
            ws.Cell(r, 1).Value = proj;
            ws.Cell(r, 2).Value = string.Join(", ", pt.Select(t => t.Name));
            ws.Cell(r, 3).Value = ids.Count;
            ws.Cell(r, 4).Value = pk.Sum(k => k.TotalHoursWorked);
            ws.Cell(r, 5).Value = pk.Sum(k => k.ExtraHours);
            ws.Cell(r, 6).Value = pk.Sum(k => k.TasksAssigned);
            ws.Cell(r, 7).Value = pk.Sum(k => k.TasksCompleted);
            r++;
        }
        ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1);
    }
}
