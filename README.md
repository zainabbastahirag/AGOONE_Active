# Monthly Man-Hours Report

A professional monthly man-hours reporting tool for tracking resource allocation across 5 projects for P&L analysis.

## Projects Covered

| # | Project |
|---|---------|
| 1 | AGONEWorkj |
| 2 | OJE Safe |
| 3 | ONe Learn |
| 4 | Ne Pulse |
| 5 | AGONE |

## Resource Types Tracked

- **Product** — Product management hours
- **UI/UX** — Design and user experience hours
- **Dev** — Development/engineering hours
- **QA** — Quality assurance and testing hours

## Quick Start

### 1. Distribute the Input Template

Share `reports/input_template_man_hours.csv` with each team lead. They fill in:

| Column | Description |
|--------|-------------|
| Project | Pre-filled project name |
| Resource Type | Pre-filled: Product, UI/UX, Dev, QA |
| Man-Hours | **Actual hours worked** (team lead fills this in) |
| Team Lead Name | Name of the person reporting |
| Validated (Y/N) | Confirmation the data is accurate |
| Notes | Any context (e.g., "resource moved from AGONE mid-month") |

### 2. Update the Script with Actual Data

Open `monthly_man_hours_report.py` and update the `JANUARY_2026_DATA` dictionary with the collected hours:

```python
JANUARY_2026_DATA = {
    "AGONEWorkj": {
        "Product": 160.0,   # Replace with actual hours
        "UI/UX": 80.0,
        "Dev": 320.0,
        "QA": 120.0,
    },
    # ... repeat for all 5 projects
}
```

### 3. Generate Reports

```bash
python3 monthly_man_hours_report.py
```

This produces the following files in the `reports/` folder:

| File | Format | Best For |
|------|--------|----------|
| `input_template_man_hours.csv` | CSV | Team leads to fill in hours |
| `man_hours_report_january_2026.csv` | CSV | Excel / Google Sheets import |
| `man_hours_report_january_2026.json` | JSON | Dashboard / system integration |
| `man_hours_report_january_2026.html` | HTML | Management presentations (print-ready) |
| `man_hours_report_january_2026.md` | Markdown | Teams / Slack sharing |

## Monthly Workflow

1. **Start of month:** Distribute `input_template_man_hours.csv` to team leads
2. **Collection:** Team leads fill in actual man-hours per project per resource type
3. **Validation:** Each team lead confirms data accuracy (even if resources moved between projects)
4. **Update script:** Enter validated data into `monthly_man_hours_report.py`
5. **Generate:** Run the script to produce all report formats
6. **Share:** Distribute HTML report to management, JSON to dashboards, Markdown to Teams

## Key Requirements

- Hours must be **accurate per product**, even if resources moved between projects mid-month
- Each team lead must **validate** their data before submission
- Monthly reporting cadence starting **January 2026**
- Data feeds into **P&L analysis** — accuracy is critical
- **January 2026 data** must be ready by next Thursday

## File Structure

```
.
├── monthly_man_hours_report.py    # Main report generator script
├── README.md                      # This file
└── reports/
    ├── input_template_man_hours.csv
    ├── man_hours_report_january_2026.csv
    ├── man_hours_report_january_2026.html
    ├── man_hours_report_january_2026.json
    └── man_hours_report_january_2026.md
```

## Requirements

- Python 3.6+ (no external dependencies required — uses only standard library)
