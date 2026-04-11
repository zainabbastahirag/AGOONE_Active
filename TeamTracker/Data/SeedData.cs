using Microsoft.AspNetCore.Identity;
using TeamTracker.Models;

namespace TeamTracker.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db, UserManager<AppUser> userManager)
    {
        if (db.Organizations.Any()) return;

        // ── 1. Create demo user + org ──
        var demoEmail = "demo@kpipulse.app";
        var demoUser = await userManager.FindByEmailAsync(demoEmail);
        if (demoUser == null)
        {
            demoUser = new AppUser
            {
                UserName = demoEmail,
                Email = demoEmail,
                DisplayName = "Zain (Tech Manager)",
                EmailConfirmed = true,
                OrgRole = AppUser.Roles.Owner,
            };
            await userManager.CreateAsync(demoUser, "Demo@12345!");
        }

        var org = new Organization
        {
            Name = "AGONE Tech Division",
            OwnerId = demoUser.Id,
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        demoUser.OrganizationId = org.Id;
        demoUser.OrgRole = AppUser.Roles.Owner;
        await userManager.UpdateAsync(demoUser);

        // ── 2. Create Teams ──
        var teams = new[]
        {
            new Team { Name = "Team Alpha",   Project = "AGONEWorkj", TechLead = "Abdullah",      OrganizationId = org.Id, Status = "Active",    StartDate = new DateTime(2025,3,1) },
            new Team { Name = "Team Beta",    Project = "OJE Safe",   TechLead = "Geena",          OrganizationId = org.Id, Status = "Active",    StartDate = new DateTime(2025,6,1) },
            new Team { Name = "Team Gamma",   Project = "ONe Learn",  TechLead = "Phuoc (Ricky)",  OrganizationId = org.Id, Status = "Active",    StartDate = new DateTime(2025,9,1) },
            new Team { Name = "Team Delta",   Project = "Ne Pulse",   TechLead = "Majed",          OrganizationId = org.Id, Status = "Active",    StartDate = new DateTime(2025,1,15) },
            new Team { Name = "Team Epsilon", Project = "AGONE",      TechLead = "Sarisha",        OrganizationId = org.Id, Status = "Active",    StartDate = new DateTime(2025,4,1) },
            new Team { Name = "Team Zeta",    Project = "AGONEWorkj", TechLead = "Faisal",         OrganizationId = org.Id, Status = "Active",    StartDate = new DateTime(2025,7,1) },
            new Team { Name = "Team Eta",     Project = "OJE Safe",   TechLead = "Hanis",          OrganizationId = org.Id, Status = "Active",    StartDate = new DateTime(2025,8,1) },
        };
        db.Teams.AddRange(teams);
        await db.SaveChangesAsync();

        // ── 3. Create Members ──
        var members = new[]
        {
            // Team Alpha — AGONEWorkj
            new Member { Name = "Abdullah",      Role = "Tech Lead",  TeamId = teams[0].Id, ProjectsAssigned = "AGONEWorkj" },
            new Member { Name = "Nastaran",      Role = "Developer",  TeamId = teams[0].Id, ProjectsAssigned = "AGONEWorkj" },
            new Member { Name = "Jawad",         Role = "Developer",  TeamId = teams[0].Id, ProjectsAssigned = "AGONEWorkj" },
            new Member { Name = "Geena",         Role = "Developer",  TeamId = teams[0].Id, ProjectsAssigned = "AGONEWorkj, OJE Safe" },
            // Team Beta — OJE Safe
            new Member { Name = "Geena",         Role = "Tech Lead",  TeamId = teams[1].Id, ProjectsAssigned = "AGONEWorkj, OJE Safe" },
            new Member { Name = "Logesh",        Role = "Developer",  TeamId = teams[1].Id, ProjectsAssigned = "OJE Safe" },
            // Team Gamma — ONe Learn
            new Member { Name = "Phuoc (Ricky)", Role = "Tech Lead",  TeamId = teams[2].Id, ProjectsAssigned = "ONe Learn" },
            new Member { Name = "Than",          Role = "Developer",  TeamId = teams[2].Id, ProjectsAssigned = "ONe Learn" },
            new Member { Name = "Loc",           Role = "Developer",  TeamId = teams[2].Id, ProjectsAssigned = "ONe Learn" },
            // Team Delta — Ne Pulse
            new Member { Name = "Majed",         Role = "Tech Lead",  TeamId = teams[3].Id, ProjectsAssigned = "Ne Pulse" },
            new Member { Name = "Hema",          Role = "Developer",  TeamId = teams[3].Id, ProjectsAssigned = "Ne Pulse" },
            new Member { Name = "Rahmya",        Role = "QA",         TeamId = teams[3].Id, ProjectsAssigned = "Ne Pulse" },
            new Member { Name = "Umeswar",       Role = "Developer",  TeamId = teams[3].Id, ProjectsAssigned = "Ne Pulse" },
            // Team Epsilon — AGONE
            new Member { Name = "Sarisha",       Role = "Tech Lead",  TeamId = teams[4].Id, ProjectsAssigned = "AGONE" },
            new Member { Name = "Kanan",         Role = "Developer",  TeamId = teams[4].Id, ProjectsAssigned = "AGONE" },
            new Member { Name = "Sharuti",       Role = "UI/UX",      TeamId = teams[4].Id, ProjectsAssigned = "AGONE" },
            // Team Zeta — AGONEWorkj
            new Member { Name = "Faisal",        Role = "Tech Lead",  TeamId = teams[5].Id, ProjectsAssigned = "AGONEWorkj" },
            new Member { Name = "Kirtinini",     Role = "Developer",  TeamId = teams[5].Id, ProjectsAssigned = "AGONEWorkj" },
            new Member { Name = "Surya",         Role = "Developer",  TeamId = teams[5].Id, ProjectsAssigned = "AGONEWorkj" },
            // Team Eta — OJE Safe
            new Member { Name = "Hanis",         Role = "Tech Lead",  TeamId = teams[6].Id, ProjectsAssigned = "OJE Safe" },
            new Member { Name = "Fatin",         Role = "Designer",   TeamId = teams[6].Id, ProjectsAssigned = "OJE Safe" },
            new Member { Name = "Max",           Role = "Developer",  TeamId = teams[6].Id, ProjectsAssigned = "OJE Safe" },
        };
        db.Members.AddRange(members);
        await db.SaveChangesAsync();

        // ── 4. Create Monthly KPIs (January 2026) ──
        var kpiData = new (int idx, int assigned, int completed, int bugsIn, int bugsFix, int reviews, int ontime, int quality, double hours, double extra,
            string achievements, string errors, string tlFeedback, string mgrFeedback, string progress)[]
        {
            // Team Alpha
            (0,  18, 16, 1, 2, 8, 92, 5, 172, 12, "Led architecture redesign for v2.0 module", "", "Excellent leadership. Drove the team through a tough sprint.", "Abdullah continues to be a pillar of the team. Promote consideration.", "On track for Q1 goals"),
            (1,  14, 12, 2, 1, 3, 78, 4, 160, 4, "Delivered payment gateway integration ahead of schedule", "Minor CSS regression on mobile checkout", "Strong delivery. Needs to write more unit tests.", "Nastaran is improving every month. Great trajectory.", "Working on API optimization"),
            (2,  12, 9,  3, 2, 2, 65, 3, 155, 8, "", "Missed edge case in user validation causing 500 errors", "Jawad needs to improve testing before PR submissions.", "Schedule a 1:1 to discuss quality improvement plan.", "Struggling with complex business logic"),
            (3,  15, 14, 0, 3, 5, 95, 5, 168, 16, "Zero bugs in 14 deliveries. Outstanding quality.", "", "Geena is our quality benchmark. Excellent across the board.", "Cross-project star performer. Consider for lead role.", "Handling dual-project load well"),
            // Team Beta
            (4,  12, 11, 1, 1, 6, 88, 4, 160, 8, "Set up CI/CD pipeline for OJE Safe", "", "Good leadership of small team. Keep pushing Logesh.", "Geena manages two teams effectively. Rare talent.", "Leading OJE Safe migration"),
            (5,  10, 6,  4, 2, 0, 55, 2, 148, 0, "", "Database migration script failed in staging. API timeout issues.", "Logesh needs significant improvement. Pair with senior dev.", "Below expectations. Consider additional training.", "Needs mentoring on SQL optimization"),
            // Team Gamma
            (6,  16, 15, 1, 3, 7, 90, 5, 175, 14, "Shipped ONe Learn mobile app v1.0 to app store", "", "Ricky is a rockstar. Delivered mobile app under pressure.", "Exceptional delivery under tight deadline. Well done.", "Mobile app launched successfully"),
            (7,  11, 10, 2, 1, 1, 82, 4, 158, 6, "Implemented real-time notifications system", "WebSocket disconnection issue under load", "Than delivers reliably. Could take on more complex tasks.", "Solid performer. Ready for more responsibility.", "Notification system live"),
            (8,  10, 8,  1, 1, 2, 75, 3, 152, 2, "", "", "Loc is consistent. Average performance, no concerns.", "Steady contributor. Encourage more initiative.", "Working on admin dashboard"),
            // Team Delta
            (9,  14, 13, 0, 4, 9, 94, 5, 170, 10, "Led successful sprint with zero production bugs", "", "Majed runs a tight ship. Best QA process in the org.", "Strong leader. Delta team has best quality metrics.", "Sprint velocity improving"),
            (10, 13, 11, 2, 3, 1, 80, 4, 162, 4, "Built patient monitoring dashboard", "Chart rendering bug on Safari", "Hema is reliable. Good output quality.", "Consistent performer. Keep up the good work.", "Dashboard in UAT"),
            (11,  8, 8,  0, 5, 4, 100, 5, 145, 0, "Caught 5 critical bugs before release through manual testing", "", "Rahmya is the reason our releases are clean. Excellent QA.", "Outstanding QA contribution. Recognition deserved.", "Zero bugs in production this month"),
            (12, 11, 7,  5, 1, 0, 58, 2, 155, 20, "", "Memory leak in background service. Unhandled exceptions in file upload.", "Umeswar is struggling. Too many bugs reaching QA.", "Needs immediate attention. Set up daily check-ins.", "High overtime indicates task estimation issues"),
            // Team Epsilon
            (13, 15, 14, 1, 2, 6, 90, 4, 165, 6, "Completed AGONE admin portal redesign", "", "Sarisha manages the team well. Clean deliveries.", "Good leadership. Team morale is high.", "Admin portal shipped"),
            (14, 12, 10, 2, 2, 1, 78, 3, 158, 8, "Implemented user analytics tracking", "Analytics data mismatch with GA4 reports", "Kanan is growing. Needs to validate data more carefully.", "Improving but still room for growth.", "Analytics module in review"),
            (15,  9, 9,  0, 0, 0, 100, 5, 140, 0, "Designed new brand guidelines and component library", "", "Sharuti's designs are exceptional. Zero revision requests.", "Best designer on the team. Valuable asset.", "Component library complete"),
            // Team Zeta
            (16, 13, 12, 1, 3, 8, 88, 4, 168, 10, "Architected microservices migration plan", "", "Faisal brings strong technical vision. Good code reviews.", "Faisal is a strong lead. Microservices plan is solid.", "Migration plan approved"),
            (17, 10, 7,  3, 1, 0, 62, 3, 150, 2, "", "API contract breaking change deployed without notice", "Kirtinini needs to communicate breaking changes better.", "Communication gap. Add to PR checklist.", "Working on service mesh"),
            (18, 11, 10, 1, 2, 2, 85, 4, 156, 4, "Implemented automated deployment pipeline", "", "Surya delivers quality work. DevOps skills improving.", "Good progress this month. Keep it up.", "CI/CD pipeline automated"),
            // Team Eta
            (19, 14, 13, 0, 2, 7, 92, 5, 170, 8, "Led security audit and fixed all critical vulnerabilities", "", "Hanis takes security seriously. Great leadership.", "Security-first mindset is exactly what OJE Safe needs.", "Security audit passed"),
            (20,  8, 8,  0, 0, 0, 100, 5, 135, 0, "Redesigned entire OJE Safe mobile UI with accessibility focus", "", "Fatin's designs are accessible and beautiful. Top tier.", "Best accessibility work in the org. Showcase this.", "Mobile redesign shipped"),
            (21, 12, 9,  4, 2, 1, 68, 3, 160, 18, "Built real-time alert notification system", "Race condition in concurrent alert processing. Memory spike under load.", "Max takes on challenging tasks but quality suffers under pressure.", "High overtime + bugs = potential burnout. Reduce scope.", "Alert system needs optimization"),
        };

        int month = DateTime.Now.Month > 1 ? DateTime.Now.Month : 1;
        int yr = DateTime.Now.Year;

        foreach (var d in kpiData)
        {
            db.MonthlyKpis.Add(new MonthlyKpi
            {
                MemberId = members[d.idx].Id,
                Year = yr,
                Month = month,
                TasksAssigned = d.assigned,
                TasksCompleted = d.completed,
                BugsFoundInWork = d.bugsIn,
                BugsFixed = d.bugsFix,
                CodeReviewsDone = d.reviews,
                OnTimeDeliveryPct = d.ontime,
                QualityScore = d.quality,
                TotalHoursWorked = d.hours,
                ExtraHours = d.extra,
                Achievements = d.achievements,
                ErrorsLog = d.errors,
                TechLeadFeedback = d.tlFeedback,
                ManagerFeedback = d.mgrFeedback,
                ProgressNotes = d.progress,
            });
        }
        await db.SaveChangesAsync();

        // ── 5. Create Daily Logs (last 10 working days) ──
        var statuses = DailyLog.StatusOptions;
        var logEntries = new (int mIdx, int daysAgo, string project, string ticket, string desc, string status, double hrs, double extra, string blockers, string notes)[]
        {
            (0,  1, "AGONEWorkj", "AGW-342", "Review and merge architecture PR for v2.0", "Completed", 8, 1, "", "Merged after 3 rounds of review"),
            (1,  1, "AGONEWorkj", "AGW-355", "Fix payment gateway timeout handling", "Completed", 7, 0, "", "Added retry logic with exponential backoff"),
            (2,  1, "AGONEWorkj", "AGW-360", "Implement user profile validation", "In Progress", 6, 0, "Waiting for UX spec on error messages", ""),
            (3,  1, "AGONEWorkj", "AGW-358", "API endpoint performance optimization", "Completed", 8, 2, "", "Reduced response time from 800ms to 120ms"),
            (4,  1, "OJE Safe",   "OJS-201", "Set up GitHub Actions CI pipeline", "Completed", 7, 0, "", "Pipeline running on all PRs now"),
            (5,  1, "OJE Safe",   "OJS-198", "Fix database migration rollback script", "Blocked", 4, 0, "Need DBA access to staging server", "Escalated to infra team"),
            (6,  1, "ONe Learn",  "ONL-150", "Submit mobile app to App Store review", "Completed", 6, 0, "", "App approved within 24 hours"),
            (7,  1, "ONe Learn",  "ONL-155", "Implement push notification service", "In Progress", 7, 1, "", "Firebase integration 80% complete"),
            (9,  1, "Ne Pulse",   "NP-420",  "Sprint planning and backlog grooming", "Completed", 5, 0, "", "Sprint 14 planned — 48 story points"),
            (10, 1, "Ne Pulse",   "NP-425",  "Build patient vitals dashboard widget", "In Progress", 8, 0, "", "Chart components rendering correctly"),
            (11, 1, "Ne Pulse",   "NP-422",  "Execute regression test suite for v3.1", "Completed", 7, 0, "", "All 245 test cases passed"),
            (12, 1, "Ne Pulse",   "NP-430",  "Fix background service memory leak", "Carry Forward", 6, 4, "Root cause unclear — need profiling tools", "Tried 3 approaches, none worked"),
            (13, 1, "AGONE",      "AG-890",  "Review admin portal wireframes with stakeholders", "Completed", 5, 0, "", "Stakeholders approved with minor changes"),
            (15, 1, "AGONE",      "AG-895",  "Create design system component library", "Completed", 8, 0, "", "40 components documented in Storybook"),
            (16, 1, "AGONEWorkj", "AGW-370", "Draft microservices migration RFC", "In Review", 7, 2, "", "RFC shared with architecture board"),
            (19, 1, "OJE Safe",   "OJS-210", "Run OWASP security scan and fix findings", "Completed", 8, 1, "", "Fixed 4 high, 8 medium vulnerabilities"),
            (20, 1, "OJE Safe",   "OJS-212", "Redesign mobile navigation with a11y", "Completed", 7, 0, "", "WCAG 2.1 AA compliance achieved"),
            (21, 1, "OJE Safe",   "OJS-215", "Build real-time alert WebSocket server", "In Progress", 8, 3, "Race condition under high concurrency", "Need to switch to actor model pattern"),

            // Day 2
            (0,  2, "AGONEWorkj", "AGW-340", "Code review for 4 team PRs", "Completed", 6, 0, "", "Found 2 security issues in Jawad's PR"),
            (1,  2, "AGONEWorkj", "AGW-352", "Integrate Stripe webhook handler", "Completed", 8, 0, "", "All payment events handled correctly"),
            (3,  2, "OJE Safe",   "OJS-195", "Build role-based access control module", "Completed", 8, 2, "", "RBAC with 4 permission levels"),
            (6,  2, "ONe Learn",  "ONL-148", "Fix deep link routing on Android", "Completed", 6, 0, "", "Tested on 8 different devices"),
            (8,  2, "ONe Learn",  "ONL-152", "Build admin content management page", "In Progress", 7, 0, "", "CRUD operations working, need pagination"),
            (9,  2, "Ne Pulse",   "NP-418",  "Conduct code review for sprint 13 PRs", "Completed", 4, 0, "", "Reviewed 6 PRs, all merged"),
            (14, 2, "AGONE",      "AG-885",  "Implement user analytics event tracking", "In Progress", 7, 1, "", "15 of 22 events instrumented"),
            (17, 2, "AGONEWorkj", "AGW-365", "Migrate user service to new API contract", "Blocked", 5, 0, "Breaking change conflicts with mobile app", "Need mobile team coordination"),
            (18, 2, "AGONEWorkj", "AGW-368", "Set up automated deployment with ArgoCD", "Completed", 7, 0, "", "Staging auto-deploy working"),

            // Day 3
            (2,  3, "AGONEWorkj", "AGW-348", "Debug 500 error in user registration flow", "Completed", 5, 0, "", "Root cause: null reference in address parser"),
            (5,  3, "OJE Safe",   "OJS-190", "Write integration tests for auth module", "Not Started", 2, 0, "Blocked on test DB provisioning", ""),
            (7,  3, "ONe Learn",  "ONL-145", "Optimize image loading with lazy load", "Completed", 6, 0, "", "Page load time reduced by 40%"),
            (10, 3, "Ne Pulse",   "NP-415",  "Create blood pressure trend chart", "Completed", 8, 0, "", "Using D3.js for smooth animations"),
            (13, 3, "AGONE",      "AG-882",  "Stakeholder demo for admin portal MVP", "Completed", 4, 0, "", "Positive feedback. Go-live approved for Feb"),
            (16, 3, "AGONEWorkj", "AGW-362", "Benchmark database query performance", "Completed", 6, 0, "", "Identified 3 slow queries — added indexes"),
            (19, 3, "OJE Safe",   "OJS-208", "Implement two-factor authentication", "Completed", 8, 0, "", "TOTP and SMS 2FA both working"),
            (21, 3, "OJE Safe",   "OJS-205", "Fix concurrent alert race condition", "In Progress", 8, 4, "Complex threading issue", "Exploring lock-free queue approach"),
        };

        var today = DateTime.Today;
        foreach (var e in logEntries)
        {
            var logDate = today.AddDays(-e.daysAgo);
            if (logDate.DayOfWeek == DayOfWeek.Saturday) logDate = logDate.AddDays(-1);
            if (logDate.DayOfWeek == DayOfWeek.Sunday) logDate = logDate.AddDays(-2);

            db.DailyLogs.Add(new DailyLog
            {
                MemberId = members[e.mIdx].Id,
                Date = logDate,
                Project = e.project,
                TaskTicket = e.ticket,
                TaskDescription = e.desc,
                Status = e.status,
                HoursWorked = e.hrs,
                ExtraHours = e.extra,
                Blockers = e.blockers,
                Notes = e.notes,
            });
        }
        await db.SaveChangesAsync();

        // ── 6. Create sample invite links ──
        db.InviteLinks.Add(new InviteLink { OrganizationId = org.Id, Role = "Viewer", Code = "demo-invite-2026" });
        db.InviteLinks.Add(new InviteLink { OrganizationId = org.Id, Role = "Manager", Code = "mgr-invite-2026", MaxUses = 5 });
        await db.SaveChangesAsync();

        // ── 7. Create notes/chat history per member ──
        var noteEntries = new (int idx, string type, string content, string author, int daysAgo)[]
        {
            (0, "Review", "Abdullah has been instrumental in driving the v2.0 architecture. His code reviews are thorough and he mentors juniors well.", "Zain", 5),
            (0, "Achievement", "Successfully led the team through a critical sprint with zero production incidents.", "Zain", 12),
            (0, "Chat", "Abdullah, can you share the architecture doc with the director before Friday?", "Zain", 2),
            (0, "Chat", "Done — shared the RFC doc in the shared drive. Let me know if he needs a walkthrough.", "Abdullah", 2),
            (1, "Note", "Nastaran is picking up speed. Her payment gateway integration was clean and well-tested.", "Abdullah", 8),
            (1, "Goal", "Target: Lead a feature independently by end of Q1 without senior oversight.", "Zain", 15),
            (2, "Concern", "Jawad's validation bug caused a production 500 error. Need to improve testing before PRs.", "Abdullah", 6),
            (2, "Chat", "Jawad, please add unit tests for all edge cases before submitting PRs. Let's review together.", "Abdullah", 5),
            (2, "Chat", "Understood. I've started writing tests for the user module. Will share by tomorrow.", "Jawad", 5),
            (3, "Achievement", "Zero bugs in 14 deliveries this month. Outstanding quality benchmark for the team.", "Abdullah", 3),
            (3, "Review", "Geena handles dual-project responsibility exceptionally well. Ready for a lead role.", "Zain", 10),
            (3, "Chat", "Geena, how are you managing the workload across both projects?", "Zain", 1),
            (3, "Chat", "It's manageable! I timebox AGONEWorkj mornings and OJE Safe afternoons. Works well so far.", "Geena", 1),
            (5, "Concern", "Logesh's database migration script failed in staging. Needs more testing rigor.", "Geena", 7),
            (5, "Goal", "Pair Logesh with a senior developer for the next 2 sprints to improve SQL skills.", "Zain", 4),
            (6, "Achievement", "Shipped ONe Learn mobile app v1.0 to App Store. Approved within 24 hours!", "Zain", 9),
            (6, "Chat", "Ricky, amazing job on the mobile launch! The client loved it.", "Zain", 8),
            (9, "Review", "Majed runs the tightest QA process in the org. Delta team has the best quality metrics.", "Zain", 11),
            (11, "Achievement", "Caught 5 critical bugs before release through manual testing. Saved us from a hotfix.", "Majed", 6),
            (12, "Concern", "Umeswar has 20 hours overtime and 5 bugs. Task estimation may be the root cause.", "Majed", 3),
            (12, "Chat", "Umeswar, let's schedule daily 15-min check-ins to help unblock you early.", "Majed", 2),
            (12, "Chat", "Thanks Majed, that would really help. I've been stuck on the file upload module.", "Umeswar", 2),
            (13, "Achievement", "Completed AGONE admin portal redesign. Stakeholders approved for Feb go-live.", "Zain", 7),
            (15, "Achievement", "Created the entire component library — 40 components in Storybook. Zero revision requests.", "Sarisha", 10),
            (15, "Review", "Sharuti's design work is exceptional. Best designer on the team. Valuable asset.", "Zain", 5),
            (16, "Note", "Faisal's microservices RFC is solid. Architecture board approved the migration plan.", "Zain", 8),
            (17, "Concern", "Kirtinini deployed a breaking API change without notifying the mobile team. Communication gap.", "Faisal", 4),
            (19, "Achievement", "Led security audit — fixed 4 high and 8 medium vulnerabilities. OWASP scan clean.", "Zain", 9),
            (20, "Achievement", "Redesigned OJE Safe mobile UI with full WCAG 2.1 AA accessibility compliance.", "Hanis", 6),
            (20, "Review", "Fatin's accessibility work is the best in the organization. Should be showcased.", "Zain", 4),
            (21, "Concern", "Max has 18h overtime + race condition bug. Potential burnout. Consider reducing scope.", "Hanis", 3),
            (21, "Chat", "Max, take Friday off. We'll redistribute the alert system tasks to the team.", "Hanis", 1),
            (21, "Chat", "Appreciate that, Hanis. I'll document the current state so others can pick it up.", "Max", 1),
        };

        var now = DateTime.UtcNow;
        foreach (var n in noteEntries)
        {
            db.Notes.Add(new Note
            {
                MemberId = members[n.idx].Id,
                Type = n.type,
                Content = n.content,
                Author = n.author,
                CreatedAt = now.AddDays(-n.daysAgo).AddHours(9 + n.daysAgo % 8),
            });
        }
        await db.SaveChangesAsync();
    }
}
