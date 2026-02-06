# Monthly Man-Hours Report — Full Year 2026

A complete man-hours tracking and reporting system for 5 projects, designed for Technical Managers reporting to Product Owners with P&L accuracy.

---

## Projects Covered

| # | Project      |
|---|--------------|
| 1 | AGONEWorkj   |
| 2 | OJE Safe     |
| 3 | ONe Learn    |
| 4 | Ne Pulse     |
| 5 | AGONE        |

## Resource Types Tracked

| Type      | Description                           |
|-----------|---------------------------------------|
| Product   | Product management hours              |
| UI/UX     | Design and user experience hours      |
| Dev       | Development / engineering hours       |
| QA        | Quality assurance and testing hours   |

---

## Best Way to Use This (Recommended Workflow)

### Your Monthly Routine (5 minutes)

```
STEP 1  →  Open yearly_man_hours_data.json
STEP 2  →  Fill in the current month's hours
STEP 3  →  Run: python3 generate_yearly_report.py
STEP 4  →  Send the HTML dashboard to the Product Owner
```

That's it. Every month, same 4 steps. The dashboard auto-calculates totals, YTD, trends, and per-project breakdowns.

---

## Quick Start

### 1. Edit the Data File

Open `yearly_man_hours_data.json` and fill in actual hours. Example for January:

```json
"January": {
    "AGONEWorkj":  { "Product": 40, "UI/UX": 60, "Dev": 320, "QA": 80 },
    "OJE Safe":    { "Product": 30, "UI/UX": 40, "Dev": 240, "QA": 60 },
    "ONe Learn":   { "Product": 20, "UI/UX": 80, "Dev": 160, "QA": 40 },
    "Ne Pulse":    { "Product": 25, "UI/UX": 30, "Dev": 200, "QA": 50 },
    "AGONE":       { "Product": 35, "UI/UX": 50, "Dev": 280, "QA": 70 }
}
```

### 2. Generate Reports

```bash
python3 generate_yearly_report.py
```

### 3. Share with Product Owner

| File | Best For |
|------|----------|
| `reports/annual_dashboard_2026.html` | **Product Owner** — open in browser, professional dashboard with tabs |
| `reports/annual_man_hours_2026.csv` | **Excel** — full year, all sections, ready for spreadsheet |
| `reports/annual_man_hours_2026_flat.csv` | **Pivot Tables / Power BI** — one row per data point, ideal for analysis |

---

## What the Dashboard Shows

The HTML dashboard has **5 tabs** the Product Owner can click through:

| Tab | What It Shows |
|-----|---------------|
| **Annual Overview** | KPI cards, bar charts by project & resource type, 12-month summary table |
| **Month-by-Month** | Detailed breakdown for each month (Project x Resource Type) |
| **Per Project** | Each project's resource allocation across all 12 months |
| **By Resource Type** | Product, UI/UX, Dev, QA hours across all projects for the year |
| **YTD Running Total** | Cumulative year-to-date hours — great for budget tracking |

Plus:
- Monthly trend mini-bar chart at the top
- KPI summary cards (total hours, active months, averages)
- Print-ready — works great as PDF via browser print

---

## Also Available: Single-Month Reports

If you need a standalone report for just one month:

```bash
python3 monthly_man_hours_report.py
```

This generates per-month outputs: CSV, JSON, HTML, and Markdown (good for Teams/Slack).

---

## Collecting Data from Team Leads

Share `reports/input_template_man_hours.csv` with each team lead. They fill in:

| Column         | Who Fills It   | Description                                      |
|----------------|----------------|--------------------------------------------------|
| Project        | Pre-filled     | Project name                                     |
| Resource Type  | Pre-filled     | Product, UI/UX, Dev, or QA                       |
| Man-Hours      | **Team Lead**  | Actual hours worked that month                   |
| Team Lead Name | **Team Lead**  | Person reporting                                 |
| Validated (Y/N)| **Team Lead**  | Confirms data is accurate                        |
| Notes          | **Team Lead**  | Context (e.g. "resource moved mid-month")        |

Generate the template:
```bash
python3 monthly_man_hours_report.py
```

---

## File Structure

```
.
├── yearly_man_hours_data.json       ← EDIT THIS (single source of truth)
├── generate_yearly_report.py        ← Run this to produce annual reports
├── monthly_man_hours_report.py      ← Run this for single-month reports
├── README.md                        ← You are here
└── reports/
    ├── annual_dashboard_2026.html   ← Share with Product Owner (HTML)
    ├── annual_man_hours_2026.csv    ← Share with Product Owner (Excel)
    ├── annual_man_hours_2026_flat.csv ← For Pivot Tables / Power BI
    ├── input_template_man_hours.csv ← For team leads to fill in
    ├── man_hours_report_january_2026.csv
    ├── man_hours_report_january_2026.html
    ├── man_hours_report_january_2026.json
    └── man_hours_report_january_2026.md
```

---

## Key Rules

- Hours must be **accurate per product**, even if resources moved between projects mid-month
- Each team lead must **validate** their numbers before you enter them
- Monthly cadence starting **January 2026**
- Data feeds into **P&L analysis** — accuracy is critical
- **January 2026 data** must be ready by next Thursday

## Requirements

- Python 3.6+ (no external packages needed — standard library only)
