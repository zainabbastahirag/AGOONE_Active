using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTracker.Data;
using TeamTracker.Models;
using TeamTracker.Services;

namespace TeamTracker.Controllers;

[Authorize]
[Route("api")]
public class ApiController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _um;
    private readonly GeminiAiService _ai;

    public ApiController(AppDbContext db, UserManager<AppUser> um, GeminiAiService ai) { _db = db; _um = um; _ai = ai; }

    private async Task<int?> OrgId()
    {
        var u = await _um.GetUserAsync(User);
        return u?.OrganizationId;
    }

    // ── Teams ──
    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams()
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var teams = await _db.Teams.Where(t => t.OrganizationId == oid)
            .Include(t => t.Members)
            .OrderBy(t => t.Name)
            .Select(t => new {
                t.Id, t.Name, t.Project, t.TechLead,
                Members = t.Members.OrderBy(m => m.Name).Select(m => new {
                    m.Id, m.Name, m.Role, m.ProjectsAssigned
                })
            }).ToListAsync();
        return Json(teams);
    }

    [HttpPost("teams")]
    public async Task<IActionResult> SaveTeam([FromBody] Team team)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        team.OrganizationId = oid.Value;
        if (team.Id == 0) _db.Teams.Add(team);
        else { var existing = await _db.Teams.FirstOrDefaultAsync(t => t.Id == team.Id && t.OrganizationId == oid);
            if (existing == null) return NotFound();
            existing.Name = team.Name; existing.Project = team.Project; existing.TechLead = team.TechLead; }
        await _db.SaveChangesAsync();
        return Json(new { ok = true, id = team.Id });
    }

    [HttpDelete("teams/{id}")]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == oid);
        if (team != null) { _db.Teams.Remove(team); await _db.SaveChangesAsync(); }
        return Json(new { ok = true });
    }

    // ── Members ──
    [HttpPost("members")]
    public async Task<IActionResult> SaveMember([FromBody] Member member)
    {
        if (member.Id == 0) _db.Members.Add(member);
        else { var existing = await _db.Members.FindAsync(member.Id);
            if (existing == null) return NotFound();
            existing.Name = member.Name; existing.Role = member.Role;
            existing.TeamId = member.TeamId; existing.ProjectsAssigned = member.ProjectsAssigned; }
        await _db.SaveChangesAsync();
        return Json(new { ok = true, id = member.Id });
    }

    [HttpDelete("members/{id}")]
    public async Task<IActionResult> DeleteMember(int id)
    {
        var m = await _db.Members.FindAsync(id);
        if (m != null) { _db.Members.Remove(m); await _db.SaveChangesAsync(); }
        return Json(new { ok = true });
    }

    [HttpPost("members/assign")]
    public async Task<IActionResult> AssignMember([FromBody] AssignDto dto)
    {
        var m = await _db.Members.FindAsync(dto.MemberId);
        if (m == null) return NotFound();
        m.TeamId = dto.TeamId;
        var team = await _db.Teams.FindAsync(dto.TeamId);
        if (team != null && !m.ProjectsAssigned.Contains(team.Project))
            m.ProjectsAssigned = string.IsNullOrEmpty(m.ProjectsAssigned)
                ? team.Project : m.ProjectsAssigned + ", " + team.Project;
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    public class AssignDto { public int MemberId { get; set; } public int TeamId { get; set; } }

    // ── Daily Log ──
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(int? month, int? year)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        int m = month ?? DateTime.Now.Month, y = year ?? DateTime.Now.Year;
        var mIds = await _db.Members.Include(x => x.Team)
            .Where(x => x.Team.OrganizationId == oid).Select(x => x.Id).ToListAsync();
        var logs = await _db.DailyLogs.Include(d => d.Member)
            .Where(d => mIds.Contains(d.MemberId) && d.Date.Month == m && d.Date.Year == y)
            .OrderByDescending(d => d.Date).ThenBy(d => d.Member.Name)
            .Select(d => new { d.Id, Date = d.Date.ToString("yyyy-MM-dd"), d.Member.Name,
                d.MemberId, d.Project, d.TaskTicket, d.TaskDescription,
                d.Status, d.HoursWorked, d.ExtraHours, d.Blockers, d.Notes })
            .Take(300).ToListAsync();
        return Json(logs);
    }

    [HttpPost("logs")]
    public async Task<IActionResult> SaveLog([FromBody] DailyLogDto dto)
    {
        DailyLog log;
        if (dto.Id == 0) { log = new DailyLog(); _db.DailyLogs.Add(log); }
        else { log = await _db.DailyLogs.FindAsync(dto.Id); if (log == null) return NotFound(); }

        log.Date = DateTime.Parse(dto.Date);
        log.MemberId = dto.MemberId;
        log.Project = dto.Project ?? "";
        log.TaskTicket = dto.TaskTicket ?? "";
        log.TaskDescription = dto.TaskDescription ?? "";
        log.Status = dto.Status ?? "In Progress";
        log.HoursWorked = dto.HoursWorked;
        log.ExtraHours = dto.ExtraHours;
        log.Blockers = dto.Blockers ?? "";
        log.Notes = dto.Notes ?? "";
        await _db.SaveChangesAsync();
        return Json(new { ok = true, id = log.Id });
    }

    public class DailyLogDto
    {
        public int Id { get; set; }
        public string Date { get; set; } = "";
        public int MemberId { get; set; }
        public string? Project { get; set; }
        public string? TaskTicket { get; set; }
        public string? TaskDescription { get; set; }
        public string? Status { get; set; }
        public double HoursWorked { get; set; }
        public double ExtraHours { get; set; }
        public string? Blockers { get; set; }
        public string? Notes { get; set; }
    }

    [HttpDelete("logs/{id}")]
    public async Task<IActionResult> DeleteLog(int id)
    {
        var l = await _db.DailyLogs.FindAsync(id);
        if (l != null) { _db.DailyLogs.Remove(l); await _db.SaveChangesAsync(); }
        return Json(new { ok = true });
    }

    // ── KPIs ──
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(int? month, int? year)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        int m = month ?? DateTime.Now.Month, y = year ?? DateTime.Now.Year;
        var mIds = await _db.Members.Include(x => x.Team)
            .Where(x => x.Team.OrganizationId == oid).Select(x => x.Id).ToListAsync();
        var kpis = await _db.MonthlyKpis.Include(k => k.Member).ThenInclude(mb => mb.Team)
            .Where(k => mIds.Contains(k.MemberId) && k.Year == y && k.Month == m)
            .OrderBy(k => k.Member.Team.Name).ThenBy(k => k.Member.Name)
            .Select(k => new { k.Id, k.MemberId, Name = k.Member.Name, Team = k.Member.Team.Name,
                Role = k.Member.Role, Project = k.Member.Team.Project,
                k.TasksAssigned, k.TasksCompleted, k.BugsFoundInWork, k.BugsFixed,
                k.CodeReviewsDone, k.OnTimeDeliveryPct, k.QualityScore,
                k.TotalHoursWorked, k.ExtraHours,
                k.Achievements, k.ErrorsLog, k.TechLeadFeedback, k.ManagerFeedback, k.ProgressNotes,
                k.CompletionPct, k.BugRatio, k.Grade })
            .ToListAsync();
        return Json(kpis);
    }

    [HttpPost("kpis/generate")]
    public async Task<IActionResult> GenerateKpis([FromBody] MonthYearDto dto)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var members = await _db.Members.Include(m => m.Team)
            .Where(m => m.Team.OrganizationId == oid).ToListAsync();
        var existing = await _db.MonthlyKpis
            .Where(k => k.Year == dto.Year && k.Month == dto.Month)
            .Select(k => k.MemberId).ToListAsync();
        int count = 0;
        foreach (var m in members)
            if (!existing.Contains(m.Id))
            { _db.MonthlyKpis.Add(new MonthlyKpi { MemberId = m.Id, Year = dto.Year, Month = dto.Month }); count++; }
        await _db.SaveChangesAsync();
        return Json(new { ok = true, created = count });
    }

    public class MonthYearDto { public int Month { get; set; } public int Year { get; set; } }

    [HttpPost("kpis")]
    public async Task<IActionResult> SaveKpi([FromBody] KpiDto dto)
    {
        var kpi = await _db.MonthlyKpis.FindAsync(dto.Id);
        if (kpi == null) return NotFound();
        kpi.TasksAssigned = dto.TasksAssigned; kpi.TasksCompleted = dto.TasksCompleted;
        kpi.BugsFoundInWork = dto.BugsFoundInWork; kpi.BugsFixed = dto.BugsFixed;
        kpi.CodeReviewsDone = dto.CodeReviewsDone; kpi.OnTimeDeliveryPct = dto.OnTimeDeliveryPct;
        kpi.QualityScore = dto.QualityScore; kpi.TotalHoursWorked = dto.TotalHoursWorked;
        kpi.ExtraHours = dto.ExtraHours; kpi.Achievements = dto.Achievements ?? "";
        kpi.ErrorsLog = dto.ErrorsLog ?? ""; kpi.TechLeadFeedback = dto.TechLeadFeedback ?? "";
        kpi.ManagerFeedback = dto.ManagerFeedback ?? ""; kpi.ProgressNotes = dto.ProgressNotes ?? "";
        await _db.SaveChangesAsync();
        return Json(new { ok = true, kpi.CompletionPct, kpi.BugRatio, kpi.Grade });
    }

    public class KpiDto
    {
        public int Id { get; set; }
        public int TasksAssigned { get; set; } public int TasksCompleted { get; set; }
        public int BugsFoundInWork { get; set; } public int BugsFixed { get; set; }
        public int CodeReviewsDone { get; set; } public int OnTimeDeliveryPct { get; set; }
        public int QualityScore { get; set; } public double TotalHoursWorked { get; set; }
        public double ExtraHours { get; set; }
        public string? Achievements { get; set; } public string? ErrorsLog { get; set; }
        public string? TechLeadFeedback { get; set; } public string? ManagerFeedback { get; set; }
        public string? ProgressNotes { get; set; }
    }

    // ── Invite Links ──
    [HttpGet("invites")]
    public async Task<IActionResult> GetInvites()
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var links = await _db.InviteLinks.Where(l => l.OrganizationId == oid)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new { l.Id, l.Code, l.Role, l.IsActive, l.MaxUses, l.TimesUsed,
                Expires = l.ExpiresAt.HasValue ? l.ExpiresAt.Value.ToString("dd MMM yyyy") : null,
                Created = l.CreatedAt.ToString("dd MMM yyyy"), l.IsValid })
            .ToListAsync();
        var members = await _db.Users.Where(u => u.OrganizationId == oid)
            .OrderBy(u => u.DisplayName)
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.OrgRole, u.AvatarUrl })
            .ToListAsync();
        return Json(new { links, members });
    }

    [HttpPost("invites")]
    public async Task<IActionResult> CreateInvite([FromBody] InviteDto dto)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var link = new InviteLink { OrganizationId = oid.Value, Role = dto.Role ?? "Viewer",
            MaxUses = dto.MaxUses, ExpiresAt = dto.ExpiryDays > 0 ? DateTime.UtcNow.AddDays(dto.ExpiryDays) : null };
        _db.InviteLinks.Add(link); await _db.SaveChangesAsync();
        return Json(new { ok = true, link.Code });
    }

    public class InviteDto { public string? Role { get; set; } public int MaxUses { get; set; } public int ExpiryDays { get; set; } }

    [HttpDelete("invites/{id}")]
    public async Task<IActionResult> DeactivateInvite(int id)
    {
        var link = await _db.InviteLinks.FindAsync(id);
        if (link != null) { link.IsActive = false; await _db.SaveChangesAsync(); }
        return Json(new { ok = true });
    }

    // ── User info ──
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var u = await _um.GetUserAsync(User);
        if (u == null) return Unauthorized();
        var orgName = u.OrganizationId.HasValue
            ? (await _db.Organizations.FindAsync(u.OrganizationId))?.Name : null;
        return Json(new { u.DisplayName, u.Email, u.AvatarUrl, u.OrgRole,
            OrgId = u.OrganizationId, OrgName = orgName, HasOrg = u.OrganizationId != null });
    }

    // ── AI Endpoints (powered by Google Gemini — free tier) ──

    [HttpGet("ai/status")]
    public IActionResult AiStatus() => Json(new { available = _ai.IsAvailable });

    [HttpPost("ai/feedback/{kpiId}")]
    public async Task<IActionResult> AiFeedback(int kpiId)
    {
        var kpi = await _db.MonthlyKpis.Include(k => k.Member).ThenInclude(m => m.Team).FirstOrDefaultAsync(k => k.Id == kpiId);
        if (kpi == null) return NotFound();
        var text = await _ai.GenerateFeedback(kpi);
        return Json(new { text, ai = _ai.IsAvailable });
    }

    [HttpPost("ai/summary")]
    public async Task<IActionResult> AiTeamSummary([FromBody] MonthYearDto dto)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var mIds = await _db.Members.Include(x => x.Team).Where(x => x.Team.OrganizationId == oid).Select(x => x.Id).ToListAsync();
        var kpis = await _db.MonthlyKpis.Include(k => k.Member).ThenInclude(m => m.Team)
            .Where(k => mIds.Contains(k.MemberId) && k.Year == dto.Year && k.Month == dto.Month).ToListAsync();
        var teams = await _db.Teams.Where(t => t.OrganizationId == oid).ToListAsync();
        var text = await _ai.GenerateTeamSummary(kpis, teams);
        return Json(new { text, ai = _ai.IsAvailable });
    }

    [HttpPost("ai/recommendations")]
    public async Task<IActionResult> AiRecommendations([FromBody] MonthYearDto dto)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var mIds = await _db.Members.Include(x => x.Team).Where(x => x.Team.OrganizationId == oid).Select(x => x.Id).ToListAsync();
        var kpis = await _db.MonthlyKpis.Include(k => k.Member).ThenInclude(m => m.Team)
            .Where(k => mIds.Contains(k.MemberId) && k.Year == dto.Year && k.Month == dto.Month).ToListAsync();
        var text = await _ai.GenerateRecommendations(kpis);
        return Json(new { text, ai = _ai.IsAvailable });
    }

    [HttpPost("ai/daily-summary")]
    public async Task<IActionResult> AiDailySummary([FromBody] DailySummaryDto dto)
    {
        var oid = await OrgId(); if (oid == null) return Unauthorized();
        var date = DateTime.Parse(dto.Date);
        var mIds = await _db.Members.Include(x => x.Team).Where(x => x.Team.OrganizationId == oid).Select(x => x.Id).ToListAsync();
        var logs = await _db.DailyLogs.Include(d => d.Member).Where(d => mIds.Contains(d.MemberId) && d.Date.Date == date.Date).ToListAsync();
        var text = await _ai.GenerateDailySummary(logs, date);
        return Json(new { text, ai = _ai.IsAvailable });
    }

    public class DailySummaryDto { public string Date { get; set; } = ""; }

    // ── Notes / Chat ──

    [HttpGet("notes/{memberId}")]
    public async Task<IActionResult> GetNotes(int memberId)
    {
        var notes = await _db.Notes.Where(n => n.MemberId == memberId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new { n.Id, n.Content, n.Type, n.Author, n.ImageUrl, Created = n.CreatedAt.ToString("dd MMM yyyy HH:mm") })
            .Take(100).ToListAsync();
        return Json(notes);
    }

    [HttpPost("notes")]
    public async Task<IActionResult> AddNote([FromBody] NoteDto dto)
    {
        var u = await _um.GetUserAsync(User);
        var note = new Note
        {
            MemberId = dto.MemberId,
            Content = dto.Content ?? "",
            Type = dto.Type ?? "Chat",
            Author = u?.DisplayName ?? "Unknown",
            ImageUrl = dto.ImageUrl,
        };
        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
        return Json(new { ok = true, id = note.Id, created = note.CreatedAt.ToString("dd MMM yyyy HH:mm"), author = note.Author });
    }

    public class NoteDto { public int MemberId { get; set; } public string? Content { get; set; } public string? Type { get; set; } public string? ImageUrl { get; set; } }

    [HttpDelete("notes/{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var n = await _db.Notes.FindAsync(id);
        if (n != null) { _db.Notes.Remove(n); await _db.SaveChangesAsync(); }
        return Json(new { ok = true });
    }

    // ── Image Upload ──

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file");
        if (file.Length > 5 * 1024 * 1024) return BadRequest("Max 5MB");

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(dir);
        var fname = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(dir, fname);
        using (var s = new FileStream(path, FileMode.Create)) await file.CopyToAsync(s);
        return Json(new { url = $"/uploads/{fname}" });
    }

    // ── Member Photo ──

    [HttpPost("members/{id}/photo")]
    public async Task<IActionResult> UploadMemberPhoto(int id, IFormFile file)
    {
        var m = await _db.Members.FindAsync(id);
        if (m == null) return NotFound();
        if (file == null || file.Length == 0) return BadRequest("No file");

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos");
        Directory.CreateDirectory(dir);
        var fname = $"member_{id}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(dir, fname);
        using (var s = new FileStream(path, FileMode.Create)) await file.CopyToAsync(s);
        m.PhotoUrl = $"/photos/{fname}";
        await _db.SaveChangesAsync();
        return Json(new { url = m.PhotoUrl });
    }

    // ── Generate Person Report (AI) ──

    [HttpPost("ai/person-report/{memberId}")]
    public async Task<IActionResult> AiPersonReport(int memberId)
    {
        var member = await _db.Members.Include(m => m.Team).FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return NotFound();
        var kpis = await _db.MonthlyKpis.Where(k => k.MemberId == memberId).OrderByDescending(k => k.Year).ThenByDescending(k => k.Month).Take(6).ToListAsync();
        var notes = await _db.Notes.Where(n => n.MemberId == memberId).OrderByDescending(n => n.CreatedAt).Take(20).ToListAsync();
        var logs = await _db.DailyLogs.Where(d => d.MemberId == memberId).OrderByDescending(d => d.Date).Take(30).ToListAsync();

        var notesSummary = string.Join("\n", notes.Select(n => $"[{n.Type}] {n.Author}: {n.Content}"));
        var kpiSummary = string.Join("\n", kpis.Select(k => $"{k.MonthName} {k.Year}: {k.TasksCompleted}/{k.TasksAssigned} tasks, Quality {k.QualityScore}/5, Grade {k.Grade}"));

        var prompt = $@"Generate a comprehensive performance report for a team member. Write 5-7 sentences.

Name: {member.Name}
Role: {member.Role}
Team: {member.Team.Name}
Project: {member.Team.Project}
Projects worked on: {member.ProjectsAssigned}

KPI History:
{(kpiSummary.Length > 0 ? kpiSummary : "No KPI data")}

Notes & Feedback:
{(notesSummary.Length > 0 ? notesSummary : "No notes")}

Recent activity: {logs.Count} log entries, {logs.Sum(l => l.HoursWorked):N0} total hours

Write a professional report covering: overall performance trajectory, key strengths, areas for development, project contributions, and recommendation for next steps. Under 200 words.";

        var text = await _ai.CallGeminiPublic(prompt);
        return Json(new { text, ai = _ai.IsAvailable });
    }
}
