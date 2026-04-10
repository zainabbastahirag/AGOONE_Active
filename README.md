# Manpower, KPI & Man-Hours Reporting System — 2026

A complete reporting toolkit for Technical Managers managing multiple teams and projects. Three integrated tools:

1. **Excel Daily Tracker** — One .xlsx file you open daily to log work, track progress, KPIs, and feedback
2. **Team KPI Dashboard** — HTML dashboard for KPI scorecards, errors, feedback, achievements
3. **Man-Hours Report** — Monthly/annual man-hours for P&L and Product Owner reporting

---

## Teams & Members

| Team   | Project      | Tech Lead      | Members                              |
|--------|-------------|----------------|--------------------------------------|
| Team 1 | AGONEWorkj  | Abdullah       | Abdullah, Nastaran, Jawad, Geena*    |
| Team 2 | OJE Safe    | Geena          | Geena*, Logesh                       |
| Team 3 | ONe Learn   | Phuoc (Ricky)  | Phuoc (Ricky), Than, Loc             |
| Team 4 | Ne Pulse    | Majed          | Majed, Hema, Rahmya, Umeswar         |
| Team 5 | AGONE       | Sarisha        | Sarisha, Kanan, Sharuti              |
| Team 6 | AGONEWorkj  | Faisal         | Faisal, Kirtinini, Surya             |
| Team 7 | OJE Safe    | Hanis          | Hanis, Fatin, Max                    |

\* = works across multiple projects (tracked separately per team)

**21 unique people** across **7 teams** and **5 projects**.

---

## TOOL 1: Excel Daily Tracker (One File — Use Every Day)

The single most important file. Open it daily, log work, track everything.

```
python3 generate_excel_tracker.py
```

Opens: `reports/team_daily_tracker_january_2026.xlsx`

### What's Inside (6 Sheets)

| Sheet | Purpose | How Often |
|-------|---------|-----------|
| **Daily Log** | Log each person's daily work — task, hours, project, status | **Every day** |
| **Team Roster** | All 21 people, teams, projects, roles at a glance | Reference |
| **Monthly KPI** | KPI scorecard with auto-formulas (completion %, bug ratio, grade) | **Weekly** |
| **Progress & Feedback** | Achievements, errors, Tech Lead + Manager feedback | **Weekly / Monthly** |
| **Project Summary** | Hours and tasks rolled up per project | Reference |
| **Instructions** | How to use this file | Reference |

### Daily Log Features
- **Dropdowns** for Name, Team, Project, Status (no typos)
- **Status options:** Not Started, In Progress, In Review, Blocked, Completed, Carry Forward
- **Auto-coloring:** Green = Completed, Red = Blocked, Blue = In Progress, Yellow = Carry Forward
- **500 pre-formatted rows** — just start typing
- **Filter by person** to see all of someone's work history
- **Filter by project** to see all work on one project

### Monthly KPI Auto-Formulas
- **Completion %** = Tasks Completed / Tasks Assigned (auto)
- **Bug Ratio** = Bugs in Work / Tasks Completed (auto)
- **Overall Grade** = Weighted A/B/C/D/F (auto — based on completion, on-time, quality, bugs)
- Green/Red conditional formatting on grades and completion rates

---

## TOOL 2: Team KPI HTML Dashboard

### What It Tracks Per Person

| Category | Fields |
|----------|--------|
| **Work Output** | Tasks assigned, tasks completed, completion rate |
| **Quality** | Bugs found in their work, bugs fixed, bug ratio, quality score (1-5) |
| **Delivery** | On-time delivery %, code reviews done |
| **Hours** | Total hours worked, extra/overtime hours |
| **Feedback** | Tech Lead feedback, Manager feedback, progress notes |
| **Recognition** | Achievements, errors log |
| **Grade** | Auto-calculated A/B/C/D/F grade based on weighted KPIs |

### How to Use (Monthly Routine)

```
STEP 1  →  Edit team_kpi_data.json (fill in KPI numbers, feedback, errors, achievements)
STEP 2  →  Run: python3 generate_team_kpi_report.py
STEP 3  →  Open reports/team_kpi_dashboard_january_2026.html in browser
```

### What the Dashboard Shows (7 Tabs)

| Tab | What You See |
|-----|-------------|
| **All Members** | Master table — every person, every KPI at a glance |
| **By Team** | Per-team breakdown with team totals |
| **KPI Scorecard** | Visual scorecards with progress bars, star ratings, auto-grades |
| **Errors & Bugs** | Bug tracking per person — bugs in work, bugs fixed, net open, error logs |
| **Feedback & Achievements** | Card layout — Tech Lead feedback, Manager feedback, achievements, progress |
| **Hours & Overtime** | Hours breakdown with overtime % and visual bars |
| **Multi-Project** | Members working across multiple projects with combined hours |

### KPI Grading System

| KPI | Weight | How It's Measured |
|-----|--------|-------------------|
| Completion Rate | 30% | Tasks Completed / Tasks Assigned |
| On-Time Delivery | 30% | % of tasks delivered before deadline |
| Quality Score | 25% | Tech Lead rating (1-5 stars) |
| Bug Ratio | 15% | Bugs in work / Tasks completed (lower = better) |

| Grade | Score Range |
|-------|-------------|
| A | 85%+ |
| B | 70-84% |
| C | 55-69% |
| D | 40-54% |
| F | Below 40% |

### Outputs

| File | Best For |
|------|----------|
| `reports/team_kpi_dashboard_<month>_<year>.html` | Open in browser — full interactive dashboard |
| `reports/team_kpi_master_<month>_<year>.csv` | Excel — all KPIs + feedback in one spreadsheet |
| `reports/team_kpi_input_template_<month>_<year>.csv` | Share with Tech Leads to fill in their team's data |
| `reports/team_kpi_flat_<month>_<year>.csv` | Pivot Tables / Power BI — one row per KPI metric |

### Collecting Data from Tech Leads

1. Share `reports/team_kpi_input_template_january_2026.csv` with each Tech Lead
2. They fill in: tasks, bugs, hours, quality score, achievements, feedback
3. You update `team_kpi_data.json` with their numbers
4. Re-run the script

### Changing the Month

Edit `team_kpi_data.json` and change `"report_month"`:

```json
"report_month": "February"
```

Then re-run `python3 generate_team_kpi_report.py`.

### Adding New Team Members or Teams

Edit `team_kpi_data.json` — copy any existing member block and change the name/role. The script auto-detects all teams and members from the JSON.

---

## TOOL 2: Man-Hours Report (for Product Owner / P&L)

### How to Use

```
STEP 1  →  Edit yearly_man_hours_data.json (fill in hours per month)
STEP 2  →  Run: python3 generate_yearly_report.py
STEP 3  →  Send reports/annual_dashboard_2026.html to Product Owner
```

### Man-Hours Dashboard (5 Tabs)

| Tab | What It Shows |
|-----|-------------|
| **Annual Overview** | KPI cards, bar charts, 12-month summary |
| **Month-by-Month** | Detailed Project x Resource Type breakdown |
| **Per Project** | Each project's resource allocation across 12 months |
| **By Resource Type** | Product, UI/UX, Dev, QA hours for the year |
| **YTD Running Total** | Cumulative hours for budget tracking |

### Single-Month Reports

```bash
python3 monthly_man_hours_report.py
```

Produces CSV, JSON, HTML, and Markdown for a single month.

---

## Projects Covered

| # | Project    |
|---|------------|
| 1 | AGONEWorkj |
| 2 | OJE Safe   |
| 3 | ONe Learn  |
| 4 | Ne Pulse   |
| 5 | AGONE      |

## Resource Types

Product, UI/UX, Dev, QA

---

## File Structure

```
.
├── team_kpi_data.json                ← EDIT THIS (people, KPIs, feedback, errors)
├── generate_excel_tracker.py         ← Run for the Excel daily tracker (.xlsx)
├── generate_team_kpi_report.py       ← Run for team KPI HTML dashboard
├── yearly_man_hours_data.json        ← EDIT THIS (monthly hours per project)
├── generate_yearly_report.py         ← Run for annual man-hours dashboard
├── monthly_man_hours_report.py       ← Run for single-month man-hours report
├── requirements.txt                  ← Python dependencies
├── README.md
└── reports/
    ├── team_daily_tracker_*.xlsx     ← THE EXCEL FILE (daily use)
    ├── team_kpi_dashboard_*.html     ← Team KPI dashboard (open in browser)
    ├── team_kpi_master_*.csv         ← Team KPI master spreadsheet
    ├── team_kpi_input_template_*.csv ← Template for Tech Leads
    ├── team_kpi_flat_*.csv           ← Pivot-ready KPI data
    ├── annual_dashboard_2026.html    ← Annual man-hours dashboard
    ├── annual_man_hours_2026.csv     ← Annual man-hours spreadsheet
    ├── annual_man_hours_2026_flat.csv
    └── ...per-month reports...
```

---

## Setup

```bash
pip install -r requirements.txt
```

## Requirements

- Python 3.6+
- `openpyxl` (for Excel generation — installed via requirements.txt)
