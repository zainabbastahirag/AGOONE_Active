# KpiPulse

**Free, lightweight Project & People Management tool for tech teams.**

Login with Google, track daily work, manage KPIs, share with your team, export to Excel — all in one app.

---

## Suggested Domain Names

Short, SEO-friendly, memorable names available for registration:

| Domain | Why It Works |
|--------|-------------|
| **kpipulse.com** | Primary pick — short, clear, SEO-strong for "KPI" searches |
| **kpipulse.io** | Tech-friendly alternative |
| **kpipulse.app** | Modern app domain |
| **kpipulse.dev** | Developer-focused |
| **kpipulse.co** | Clean, startup-style |
| **mypulseapp.com** | Broader appeal |
| **teampulse.app** | Team-focused alternative |
| **pulsetrack.io** | Tracking-focused |
| **kpihub.io** | Hub for KPIs |
| **crewpulse.com** | Crew/team management angle |

**Recommendation:** Register `kpipulse.com` as primary + `kpipulse.io` as backup.

---

## Features

- **Google SSO Login** — mandatory Gmail/Google sign-in, no passwords
- **Multi-user with Roles** — Owner, Manager, Viewer
- **Shareable Invite Links** — generate a link, share via Teams/Slack/WhatsApp, anyone can join
- **Organization-scoped** — each org sees only their own data
- **Daily Work Log** — log tasks per person per day with status tracking
- **KPI Scorecard** — auto-graded A/B/C/D/F with weighted formulas
- **Team & Member Management** — add/edit teams, members, projects
- **Feedback & Achievements** — Tech Lead + Manager feedback per person
- **Error/Bug Tracking** — per-person bug ratio and error logs
- **Excel Export** — download full reports, daily logs, or KPI scorecards as .xlsx anytime
- **SQL Server + SQLite** — SQL Server for production (IIS), SQLite for local dev
- **EF Core Code-First** — auto-migrations on startup
- **IIS Ready** — includes web.config for direct IIS deployment

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core 8 MVC |
| Database | SQL Server (prod) / SQLite (dev) |
| ORM | Entity Framework Core 8 (Code-First) |
| Auth | Google SSO via ASP.NET Identity |
| Excel | ClosedXML |
| Frontend | Bootstrap 5 + Bootstrap Icons |
| Hosting | IIS / Azure / any .NET host |

---

## Quick Start (Local Dev)

```bash
cd TeamTracker
dotnet restore
dotnet ef database update
dotnet run
```

Open `http://localhost:5050` — you'll see the Google login page.

### Local Dev with SQLite (no SQL Server needed)

Leave `SqlServer` connection string empty in `appsettings.json` — the app auto-falls back to SQLite.

---

## Google SSO Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a project (or select existing)
3. Go to **APIs & Services > Credentials**
4. Click **Create Credentials > OAuth 2.0 Client ID**
5. Set **Authorized redirect URIs** to: `https://yourdomain.com/signin-google`
6. Copy the **Client ID** and **Client Secret**
7. Update `appsettings.json` or `appsettings.Production.json`:

```json
"Authentication": {
    "Google": {
        "ClientId": "your-client-id.apps.googleusercontent.com",
        "ClientSecret": "your-client-secret"
    }
}
```

---

## SQL Server Setup (Production)

Update `appsettings.Production.json`:

```json
"ConnectionStrings": {
    "SqlServer": "Server=YOUR_SERVER;Database=KpiPulse;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

The app auto-creates the database and runs migrations on startup.

---

## IIS Deployment

1. **Publish:**
```bash
dotnet publish -c Release -o ./publish
```

2. **Copy** the `publish` folder to your IIS server

3. **Create IIS Site** pointing to the publish folder

4. **Install** the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)

5. **Set App Pool** to "No Managed Code"

6. The `web.config` is already included and configured

---

## How It Works

### First-Time Setup

1. User visits the app and signs in with Google
2. First user creates an **Organization** (e.g., "AGONE Tech Team")
3. They become the **Owner** with full access

### Inviting Others

1. Owner goes to **Invite & Share**
2. Creates a shareable link with a role (Viewer or Manager)
3. Shares the link via email/Teams/Slack/WhatsApp
4. Anyone with the link signs in with Google and joins the org

### Daily Usage

1. **Daily Log** — add entries for what each person worked on
2. **Monthly KPI** — generate KPI records, fill in numbers, grades auto-calculate
3. **Export** — download Excel reports anytime

### Roles

| Role | Can View | Can Edit | Can Invite | Can Manage Roles |
|------|----------|----------|------------|-----------------|
| Owner | Yes | Yes | Yes | Yes |
| Manager | Yes | Yes | Yes | No |
| Viewer | Yes | No | No | No |

---

## File Structure

```
TeamTracker/
├── Controllers/
│   ├── AuthController.cs         ← Google login, org setup, invite join
│   ├── HomeController.cs         ← Dashboard
│   ├── DailyLogController.cs     ← Daily work CRUD
│   ├── KpiController.cs          ← Monthly KPI management
│   ├── MembersController.cs      ← Team member CRUD
│   ├── TeamsController.cs        ← Team CRUD
│   ├── InviteController.cs       ← Shareable invite links
│   ├── ExportController.cs       ← Excel export (ClosedXML)
│   └── BaseOrgController.cs      ← Org-scoped base controller
├── Models/
│   ├── AppUser.cs                ← Identity user with org + role
│   ├── Organization.cs           ← Multi-tenant org
│   ├── InviteLink.cs             ← Shareable invite codes
│   ├── Team.cs, Member.cs        ← Team structure
│   ├── DailyLog.cs               ← Daily work entries
│   └── MonthlyKpi.cs             ← KPI with auto-grade
├── Data/
│   └── AppDbContext.cs           ← EF Core context (Identity + app data)
├── Views/                         ← Razor views (Bootstrap 5 UI)
├── appsettings.json              ← Dev config (SQLite)
├── appsettings.Production.json   ← Prod config (SQL Server)
├── web.config                    ← IIS hosting config
└── Program.cs                    ← App startup with auth + EF
```

---

## Requirements

- .NET 8 SDK
- SQL Server (production) or SQLite (development)
- Google Cloud OAuth credentials
