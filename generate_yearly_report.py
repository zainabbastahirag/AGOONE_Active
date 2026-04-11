#!/usr/bin/env python3
"""
Full-Year Man-Hours Report Generator (2026)
=============================================
Generates a complete annual man-hours tracking report for the Product Owner.

- Reads data from yearly_man_hours_data.json (single source of truth)
- Produces: Full-Year CSV, HTML Dashboard, per-project summaries
- Covers all 12 months, 5 projects, 4 resource types

Products: AGONEWorkj, OJE Safe, ONe Learn, Ne Pulse, AGONE
Resource Types: Product, UI/UX, Dev, QA

Usage:
    python3 generate_yearly_report.py

    To update data, edit yearly_man_hours_data.json and re-run.
"""

import csv
import json
import os
from datetime import datetime
from collections import defaultdict

# ─────────────────────────────────────────────────────────────────────────────
# CONSTANTS
# ─────────────────────────────────────────────────────────────────────────────

MONTHS = [
    "January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December",
]

MONTH_SHORT = [
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
]

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DATA_FILE = os.path.join(SCRIPT_DIR, "yearly_man_hours_data.json")
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "reports")


# ─────────────────────────────────────────────────────────────────────────────
# DATA LOADING
# ─────────────────────────────────────────────────────────────────────────────

def load_data():
    """Load the yearly data from JSON file."""
    with open(DATA_FILE, "r") as f:
        return json.load(f)


def get_hours(data, month, project, rtype):
    """Safely get hours for a given month/project/resource type."""
    return data.get("monthly_data", {}).get(month, {}).get(project, {}).get(rtype, 0)


# ─────────────────────────────────────────────────────────────────────────────
# CALCULATIONS
# ─────────────────────────────────────────────────────────────────────────────

def calc_monthly_project_total(data, month, project):
    """Total hours for one project in one month."""
    return sum(get_hours(data, month, project, rt) for rt in data["resource_types"])


def calc_monthly_resource_total(data, month, rtype):
    """Total hours for one resource type across all projects in one month."""
    return sum(get_hours(data, month, p, rtype) for p in data["projects"])


def calc_monthly_grand_total(data, month):
    """Total hours across all projects and resource types in one month."""
    return sum(calc_monthly_project_total(data, month, p) for p in data["projects"])


def calc_ytd_project_total(data, project, up_to_month_idx):
    """Year-to-date total for a project."""
    return sum(calc_monthly_project_total(data, MONTHS[i], project) for i in range(up_to_month_idx + 1))


def calc_ytd_resource_total(data, rtype, up_to_month_idx):
    """Year-to-date total for a resource type."""
    return sum(calc_monthly_resource_total(data, MONTHS[i], rtype) for i in range(up_to_month_idx + 1))


def calc_ytd_grand_total(data, up_to_month_idx):
    """Year-to-date grand total."""
    return sum(calc_monthly_grand_total(data, MONTHS[i]) for i in range(up_to_month_idx + 1))


def calc_annual_project_total(data, project):
    """Full-year total for a project."""
    return calc_ytd_project_total(data, project, 11)


def calc_annual_resource_total(data, rtype):
    """Full-year total for a resource type."""
    return calc_ytd_resource_total(data, rtype, 11)


def calc_annual_grand_total(data):
    """Full-year grand total."""
    return calc_ytd_grand_total(data, 11)


# ─────────────────────────────────────────────────────────────────────────────
# CSV: FULL-YEAR MASTER TEMPLATE
# ─────────────────────────────────────────────────────────────────────────────

def generate_full_year_csv(data, output_path):
    """
    Generate a master Excel-compatible CSV with all 12 months.
    Layout: One section per month, plus annual summary.
    """
    projects = data["projects"]
    rtypes = data["resource_types"]

    with open(output_path, "w", newline="") as f:
        w = csv.writer(f)

        # Title
        w.writerow([f"ANNUAL MAN-HOURS REPORT — {data['year']}"])
        w.writerow([f"Generated: {datetime.now().strftime('%B %d, %Y %H:%M')}"])
        w.writerow([f"Projects: {', '.join(projects)}"])
        w.writerow([])

        # ── Section 1: Month-by-Month Detail ──
        w.writerow(["=" * 80])
        w.writerow(["SECTION 1: MONTHLY DETAIL"])
        w.writerow(["=" * 80])
        w.writerow([])

        for month in MONTHS:
            w.writerow([f"--- {month} {data['year']} ---"])
            w.writerow(["Project"] + rtypes + ["Monthly Total"])

            for project in projects:
                row = [project]
                for rt in rtypes:
                    row.append(get_hours(data, month, project, rt))
                row.append(calc_monthly_project_total(data, month, project))
                w.writerow(row)

            # Monthly totals row
            total_row = ["MONTHLY TOTAL"]
            for rt in rtypes:
                total_row.append(calc_monthly_resource_total(data, month, rt))
            total_row.append(calc_monthly_grand_total(data, month))
            w.writerow(total_row)
            w.writerow([])

        # ── Section 2: Annual Summary by Project ──
        w.writerow(["=" * 80])
        w.writerow(["SECTION 2: ANNUAL SUMMARY BY PROJECT"])
        w.writerow(["=" * 80])
        w.writerow([])
        w.writerow(["Project"] + MONTH_SHORT + ["ANNUAL TOTAL"])

        for project in projects:
            row = [project]
            for i, month in enumerate(MONTHS):
                row.append(calc_monthly_project_total(data, month, project))
            row.append(calc_annual_project_total(data, project))
            w.writerow(row)

        total_row = ["TOTAL"]
        for i, month in enumerate(MONTHS):
            total_row.append(calc_monthly_grand_total(data, month))
        total_row.append(calc_annual_grand_total(data))
        w.writerow(total_row)
        w.writerow([])

        # ── Section 3: Annual Summary by Resource Type ──
        w.writerow(["=" * 80])
        w.writerow(["SECTION 3: ANNUAL SUMMARY BY RESOURCE TYPE"])
        w.writerow(["=" * 80])
        w.writerow([])
        w.writerow(["Resource Type"] + MONTH_SHORT + ["ANNUAL TOTAL"])

        for rt in rtypes:
            row = [rt]
            for month in MONTHS:
                row.append(calc_monthly_resource_total(data, month, rt))
            row.append(calc_annual_resource_total(data, rt))
            w.writerow(row)

        total_row = ["TOTAL"]
        for month in MONTHS:
            total_row.append(calc_monthly_grand_total(data, month))
        total_row.append(calc_annual_grand_total(data))
        w.writerow(total_row)
        w.writerow([])

        # ── Section 4: Per-Project Breakdown (one block per project) ──
        w.writerow(["=" * 80])
        w.writerow(["SECTION 4: PER-PROJECT RESOURCE BREAKDOWN"])
        w.writerow(["=" * 80])
        w.writerow([])

        for project in projects:
            w.writerow([f"--- {project} ---"])
            w.writerow(["Resource Type"] + MONTH_SHORT + ["ANNUAL TOTAL"])
            for rt in rtypes:
                row = [rt]
                for month in MONTHS:
                    row.append(get_hours(data, month, project, rt))
                row.append(sum(get_hours(data, month, project, rt) for month in MONTHS))
                w.writerow(row)
            # Project total
            row = ["PROJECT TOTAL"]
            for month in MONTHS:
                row.append(calc_monthly_project_total(data, month, project))
            row.append(calc_annual_project_total(data, project))
            w.writerow(row)
            w.writerow([])

    print(f"  Full-year CSV saved to: {output_path}")


# ─────────────────────────────────────────────────────────────────────────────
# CSV: FLAT DATA (for pivot tables / BI tools)
# ─────────────────────────────────────────────────────────────────────────────

def generate_flat_csv(data, output_path):
    """
    Generate a flat/normalized CSV — best for Excel pivot tables, Power BI, etc.
    One row per Month x Project x Resource Type.
    """
    projects = data["projects"]
    rtypes = data["resource_types"]

    with open(output_path, "w", newline="") as f:
        w = csv.writer(f)
        w.writerow(["Year", "Month", "Month#", "Project", "Resource Type", "Man-Hours"])

        for i, month in enumerate(MONTHS):
            for project in projects:
                for rt in rtypes:
                    w.writerow([
                        data["year"],
                        month,
                        i + 1,
                        project,
                        rt,
                        get_hours(data, month, project, rt),
                    ])

    print(f"  Flat CSV (pivot-ready) saved to: {output_path}")


# ─────────────────────────────────────────────────────────────────────────────
# HTML: FULL-YEAR INTERACTIVE DASHBOARD
# ─────────────────────────────────────────────────────────────────────────────

def generate_yearly_html(data, output_path):
    """Generate a professional full-year HTML dashboard with tabs for each view."""
    projects = data["projects"]
    rtypes = data["resource_types"]
    year = data["year"]

    # Pre-compute all values
    monthly_totals = [calc_monthly_grand_total(data, m) for m in MONTHS]
    annual_total = sum(monthly_totals)

    # Determine which months have data
    months_with_data = [i for i, t in enumerate(monthly_totals) if t > 0]
    active_months_count = len(months_with_data) if months_with_data else 0
    avg_monthly = annual_total / active_months_count if active_months_count > 0 else 0

    # Project annual totals for ranking
    project_annual = {p: calc_annual_project_total(data, p) for p in projects}
    resource_annual = {rt: calc_annual_resource_total(data, rt) for rt in rtypes}

    # Build monthly data as JSON for inline JS
    js_monthly_totals = json.dumps(monthly_totals)
    js_project_monthly = json.dumps({
        p: [calc_monthly_project_total(data, m, p) for m in MONTHS]
        for p in projects
    })

    # Color palette for projects
    project_colors = ["#3182ce", "#e53e3e", "#38a169", "#d69e2e", "#805ad5"]

    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Annual Man-Hours Dashboard — {year}</title>
<style>
  :root {{
    --primary: #1a365d;
    --primary-light: #2b6cb0;
    --accent: #3182ce;
    --bg: #f0f4f8;
    --card: #ffffff;
    --border: #e2e8f0;
    --text: #2d3748;
    --text-light: #718096;
    --text-lighter: #a0aec0;
    --success: #38a169;
    --danger: #e53e3e;
    --warning: #d69e2e;
    --purple: #805ad5;
    --radius: 10px;
    --shadow: 0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.06);
    --shadow-lg: 0 10px 15px -3px rgba(0,0,0,0.08), 0 4px 6px -2px rgba(0,0,0,0.04);
  }}
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  body {{
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    background: var(--bg);
    color: var(--text);
    line-height: 1.5;
  }}

  /* ── Layout ── */
  .top-bar {{
    background: linear-gradient(135deg, var(--primary) 0%, var(--primary-light) 100%);
    color: white;
    padding: 1.5rem 2rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    position: sticky;
    top: 0;
    z-index: 100;
    box-shadow: var(--shadow-lg);
  }}
  .top-bar h1 {{ font-size: 1.35rem; font-weight: 700; }}
  .top-bar .meta {{ font-size: 0.8rem; opacity: 0.85; text-align: right; }}
  .top-bar .meta .year-badge {{
    display: inline-block;
    background: rgba(255,255,255,0.2);
    padding: 0.2rem 0.75rem;
    border-radius: 20px;
    font-weight: 600;
    font-size: 0.9rem;
    margin-bottom: 0.25rem;
  }}
  .container {{
    max-width: 1280px;
    margin: 0 auto;
    padding: 1.5rem;
  }}

  /* ── KPI Cards ── */
  .kpi-row {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
    margin-bottom: 1.5rem;
  }}
  .kpi {{
    background: var(--card);
    border-radius: var(--radius);
    padding: 1.25rem 1.5rem;
    box-shadow: var(--shadow);
    border-left: 4px solid var(--accent);
  }}
  .kpi.green {{ border-left-color: var(--success); }}
  .kpi.red {{ border-left-color: var(--danger); }}
  .kpi.yellow {{ border-left-color: var(--warning); }}
  .kpi.purple {{ border-left-color: var(--purple); }}
  .kpi .kpi-label {{
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--text-light);
    margin-bottom: 0.25rem;
  }}
  .kpi .kpi-value {{
    font-size: 1.75rem;
    font-weight: 800;
    color: var(--primary);
    line-height: 1.2;
  }}
  .kpi .kpi-sub {{
    font-size: 0.75rem;
    color: var(--text-lighter);
    margin-top: 0.15rem;
  }}

  /* ── Tabs ── */
  .tabs {{
    display: flex;
    gap: 0;
    border-bottom: 2px solid var(--border);
    margin-bottom: 1.5rem;
    overflow-x: auto;
  }}
  .tab {{
    padding: 0.75rem 1.25rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-light);
    cursor: pointer;
    border-bottom: 3px solid transparent;
    transition: all 0.2s;
    white-space: nowrap;
    user-select: none;
  }}
  .tab:hover {{ color: var(--primary); background: rgba(49,130,206,0.04); }}
  .tab.active {{
    color: var(--accent);
    border-bottom-color: var(--accent);
  }}
  .tab-content {{ display: none; }}
  .tab-content.active {{ display: block; }}

  /* ── Cards / Panels ── */
  .panel {{
    background: var(--card);
    border-radius: var(--radius);
    box-shadow: var(--shadow);
    margin-bottom: 1.5rem;
    overflow: hidden;
  }}
  .panel-header {{
    padding: 1rem 1.5rem;
    font-weight: 700;
    font-size: 0.95rem;
    color: var(--primary);
    border-bottom: 1px solid var(--border);
    background: #f8fafc;
  }}
  .panel-body {{
    padding: 1.25rem 1.5rem;
    overflow-x: auto;
  }}

  /* ── Tables ── */
  table {{ width: 100%; border-collapse: collapse; font-size: 0.85rem; }}
  th {{
    background: var(--primary);
    color: white;
    padding: 0.7rem 0.75rem;
    text-align: center;
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    font-weight: 600;
    white-space: nowrap;
    position: sticky;
    top: 0;
  }}
  th:first-child {{ text-align: left; }}
  td {{
    padding: 0.6rem 0.75rem;
    text-align: center;
    border-bottom: 1px solid var(--border);
    white-space: nowrap;
  }}
  td:first-child {{
    text-align: left;
    font-weight: 600;
    color: var(--primary);
    position: sticky;
    left: 0;
    background: inherit;
    z-index: 1;
  }}
  tbody tr:hover {{ background: #edf2f7; }}
  tbody tr:nth-child(even) {{ background: #fafbfc; }}
  tbody tr:nth-child(even):hover {{ background: #edf2f7; }}
  tfoot td {{
    font-weight: 700;
    background: #edf2f7;
    border-top: 2px solid var(--primary);
    padding: 0.75rem;
  }}
  tfoot td:first-child {{ background: #edf2f7; }}
  .zero {{ color: var(--text-lighter); }}
  .has-val {{ font-weight: 600; color: var(--text); }}
  .total-col {{ background: #f0f4f8 !important; font-weight: 700; color: var(--primary) !important; }}

  /* ── Bar chart (pure CSS) ── */
  .bar-chart {{
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
  }}
  .bar-row {{
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }}
  .bar-label {{
    width: 100px;
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--primary);
    text-align: right;
    flex-shrink: 0;
  }}
  .bar-track {{
    flex: 1;
    background: #edf2f7;
    border-radius: 6px;
    height: 28px;
    position: relative;
    overflow: hidden;
  }}
  .bar-fill {{
    height: 100%;
    border-radius: 6px;
    display: flex;
    align-items: center;
    padding-left: 0.5rem;
    font-size: 0.75rem;
    font-weight: 700;
    color: white;
    transition: width 0.5s ease;
    min-width: fit-content;
  }}
  .bar-value {{
    width: 70px;
    text-align: right;
    font-size: 0.8rem;
    font-weight: 700;
    color: var(--text);
    flex-shrink: 0;
  }}

  /* ── Monthly mini-bars ── */
  .mini-bars {{
    display: grid;
    grid-template-columns: repeat(12, 1fr);
    gap: 4px;
    align-items: end;
    height: 120px;
    padding: 0.5rem 0;
  }}
  .mini-bar-col {{
    display: flex;
    flex-direction: column;
    align-items: center;
    height: 100%;
    justify-content: flex-end;
  }}
  .mini-bar {{
    width: 100%;
    border-radius: 4px 4px 0 0;
    background: var(--accent);
    min-height: 2px;
    transition: height 0.3s;
  }}
  .mini-bar-label {{
    font-size: 0.65rem;
    color: var(--text-light);
    margin-top: 4px;
    font-weight: 600;
  }}
  .mini-bar-val {{
    font-size: 0.65rem;
    color: var(--text);
    font-weight: 700;
    margin-bottom: 2px;
  }}

  /* ── Project badge colors ── */
  .badge {{
    display: inline-block;
    padding: 0.15rem 0.5rem;
    border-radius: 12px;
    font-size: 0.7rem;
    font-weight: 600;
    color: white;
  }}

  /* ── Print ── */
  @media print {{
    body {{ background: white; }}
    .top-bar {{ position: static; }}
    .tabs {{ display: none; }}
    .tab-content {{ display: block !important; page-break-inside: avoid; }}
    .panel {{ box-shadow: none; border: 1px solid #ddd; }}
  }}

  /* ── Responsive ── */
  @media (max-width: 768px) {{
    .container {{ padding: 0.75rem; }}
    .kpi-row {{ grid-template-columns: repeat(2, 1fr); }}
    .top-bar {{ flex-direction: column; text-align: center; gap: 0.5rem; }}
    .top-bar .meta {{ text-align: center; }}
  }}
</style>
</head>
<body>

<!-- ═══════════════ TOP BAR ═══════════════ -->
<div class="top-bar">
  <div>
    <h1>Man-Hours Annual Dashboard</h1>
    <div style="font-size:0.8rem; opacity:0.8; margin-top:0.15rem;">Resource Allocation &amp; P&amp;L Tracking</div>
  </div>
  <div class="meta">
    <div class="year-badge">{year}</div>
    <div>Generated: {datetime.now().strftime("%b %d, %Y %H:%M")}</div>
  </div>
</div>

<div class="container">

  <!-- ═══════════════ KPI ROW ═══════════════ -->
  <div class="kpi-row">
    <div class="kpi green">
      <div class="kpi-label">Annual Total Hours</div>
      <div class="kpi-value">{annual_total:,.0f}</div>
      <div class="kpi-sub">across all projects</div>
    </div>
    <div class="kpi">
      <div class="kpi-label">Active Months</div>
      <div class="kpi-value">{active_months_count}</div>
      <div class="kpi-sub">of 12 months reported</div>
    </div>
    <div class="kpi yellow">
      <div class="kpi-label">Avg Monthly Hours</div>
      <div class="kpi-value">{avg_monthly:,.0f}</div>
      <div class="kpi-sub">per active month</div>
    </div>
    <div class="kpi purple">
      <div class="kpi-label">Projects Tracked</div>
      <div class="kpi-value">{len(projects)}</div>
      <div class="kpi-sub">{', '.join(rtypes)}</div>
    </div>
"""

    # Per-resource KPIs
    for i, rt in enumerate(rtypes):
        colors = ["", "red", "green", "yellow"]
        html += f"""    <div class="kpi {colors[i % len(colors)]}">
      <div class="kpi-label">{rt} — Annual</div>
      <div class="kpi-value">{resource_annual[rt]:,.0f}</div>
      <div class="kpi-sub">hours total</div>
    </div>
"""

    html += """  </div>

  <!-- ═══════════════ MONTHLY TREND (mini-bars) ═══════════════ -->
  <div class="panel">
    <div class="panel-header">Monthly Hours Trend</div>
    <div class="panel-body">
      <div class="mini-bars">
"""

    max_monthly = max(monthly_totals) if max(monthly_totals) > 0 else 1
    for i, (ms, total) in enumerate(zip(MONTH_SHORT, monthly_totals)):
        pct = (total / max_monthly * 100) if max_monthly > 0 else 0
        html += f"""        <div class="mini-bar-col">
          <div class="mini-bar-val">{total:,.0f}</div>
          <div class="mini-bar" style="height:{max(pct, 2)}%; background:{project_colors[i % len(project_colors)]};"></div>
          <div class="mini-bar-label">{ms}</div>
        </div>
"""

    html += """      </div>
    </div>
  </div>

  <!-- ═══════════════ TAB NAVIGATION ═══════════════ -->
  <div class="tabs" id="mainTabs">
    <div class="tab active" data-tab="tab-overview">Annual Overview</div>
    <div class="tab" data-tab="tab-monthly">Month-by-Month</div>
    <div class="tab" data-tab="tab-projects">Per Project</div>
    <div class="tab" data-tab="tab-resources">By Resource Type</div>
    <div class="tab" data-tab="tab-ytd">YTD Running Total</div>
  </div>

  <!-- ═══════════════ TAB 1: ANNUAL OVERVIEW ═══════════════ -->
  <div class="tab-content active" id="tab-overview">

    <!-- Project totals bar chart -->
    <div class="panel">
      <div class="panel-header">Annual Hours by Project</div>
      <div class="panel-body">
        <div class="bar-chart">
"""

    max_proj = max(project_annual.values()) if project_annual and max(project_annual.values()) > 0 else 1
    for i, p in enumerate(projects):
        pct = (project_annual[p] / max_proj * 100) if max_proj > 0 else 0
        html += f"""          <div class="bar-row">
            <div class="bar-label">{p}</div>
            <div class="bar-track">
              <div class="bar-fill" style="width:{max(pct, 1)}%; background:{project_colors[i]};">&nbsp;</div>
            </div>
            <div class="bar-value">{project_annual[p]:,.0f}h</div>
          </div>
"""

    html += """        </div>
      </div>
    </div>

    <!-- Resource type bar chart -->
    <div class="panel">
      <div class="panel-header">Annual Hours by Resource Type</div>
      <div class="panel-body">
        <div class="bar-chart">
"""

    rt_colors = ["#3182ce", "#e53e3e", "#38a169", "#d69e2e"]
    max_rt = max(resource_annual.values()) if resource_annual and max(resource_annual.values()) > 0 else 1
    for i, rt in enumerate(rtypes):
        pct = (resource_annual[rt] / max_rt * 100) if max_rt > 0 else 0
        html += f"""          <div class="bar-row">
            <div class="bar-label">{rt}</div>
            <div class="bar-track">
              <div class="bar-fill" style="width:{max(pct, 1)}%; background:{rt_colors[i]};">&nbsp;</div>
            </div>
            <div class="bar-value">{resource_annual[rt]:,.0f}h</div>
          </div>
"""

    html += """        </div>
      </div>
    </div>

    <!-- Summary table: project x month -->
    <div class="panel">
      <div class="panel-header">Project Hours by Month</div>
      <div class="panel-body">
        <table>
          <thead><tr><th>Project</th>
"""
    for ms in MONTH_SHORT:
        html += f"<th>{ms}</th>"
    html += "<th>ANNUAL</th></tr></thead><tbody>\n"

    for i, p in enumerate(projects):
        html += f'<tr><td>{p}</td>'
        for month in MONTHS:
            v = calc_monthly_project_total(data, month, p)
            cls = "has-val" if v > 0 else "zero"
            html += f'<td class="{cls}">{v:,.0f}</td>'
        html += f'<td class="total-col">{project_annual[p]:,.0f}</td></tr>\n'

    html += '<tr><td></td>'  # spacer
    for month in MONTHS:
        html += '<td></td>'
    html += '<td></td></tr>\n'

    html += "</tbody><tfoot><tr><td>TOTAL</td>"
    for month in MONTHS:
        html += f'<td>{calc_monthly_grand_total(data, month):,.0f}</td>'
    html += f'<td class="total-col">{annual_total:,.0f}</td></tr></tfoot></table>'
    html += """
      </div>
    </div>
  </div>

  <!-- ═══════════════ TAB 2: MONTH-BY-MONTH ═══════════════ -->
  <div class="tab-content" id="tab-monthly">
"""

    for mi, month in enumerate(MONTHS):
        mtotal = calc_monthly_grand_total(data, month)
        html += f"""    <div class="panel">
      <div class="panel-header">{month} {year} — Total: {mtotal:,.0f} hours</div>
      <div class="panel-body">
        <table>
          <thead><tr><th>Project</th>"""
        for rt in rtypes:
            html += f"<th>{rt}</th>"
        html += "<th>Total</th></tr></thead><tbody>\n"

        for p in projects:
            html += f"<tr><td>{p}</td>"
            for rt in rtypes:
                v = get_hours(data, month, p, rt)
                cls = "has-val" if v > 0 else "zero"
                html += f'<td class="{cls}">{v:,.0f}</td>'
            pt = calc_monthly_project_total(data, month, p)
            cls = "has-val" if pt > 0 else "zero"
            html += f'<td class="{cls}">{pt:,.0f}</td></tr>\n'

        html += "</tbody><tfoot><tr><td>TOTAL</td>"
        for rt in rtypes:
            html += f"<td>{calc_monthly_resource_total(data, month, rt):,.0f}</td>"
        html += f"<td>{mtotal:,.0f}</td></tr></tfoot></table>"
        html += """
      </div>
    </div>
"""

    html += """  </div>

  <!-- ═══════════════ TAB 3: PER PROJECT ═══════════════ -->
  <div class="tab-content" id="tab-projects">
"""

    for pi, p in enumerate(projects):
        p_total = calc_annual_project_total(data, p)
        html += f"""    <div class="panel">
      <div class="panel-header" style="border-left:4px solid {project_colors[pi]};">{p} — Annual: {p_total:,.0f} hours</div>
      <div class="panel-body">
        <table>
          <thead><tr><th>Resource</th>"""
        for ms in MONTH_SHORT:
            html += f"<th>{ms}</th>"
        html += "<th>ANNUAL</th></tr></thead><tbody>\n"

        for rt in rtypes:
            html += f"<tr><td>{rt}</td>"
            rt_annual = 0
            for month in MONTHS:
                v = get_hours(data, month, p, rt)
                cls = "has-val" if v > 0 else "zero"
                html += f'<td class="{cls}">{v:,.0f}</td>'
                rt_annual += v
            html += f'<td class="total-col">{rt_annual:,.0f}</td></tr>\n'

        html += "</tbody><tfoot><tr><td>TOTAL</td>"
        for month in MONTHS:
            html += f"<td>{calc_monthly_project_total(data, month, p):,.0f}</td>"
        html += f'<td class="total-col">{p_total:,.0f}</td></tr></tfoot></table>'
        html += """
      </div>
    </div>
"""

    html += """  </div>

  <!-- ═══════════════ TAB 4: BY RESOURCE TYPE ═══════════════ -->
  <div class="tab-content" id="tab-resources">
"""

    for ri, rt in enumerate(rtypes):
        rt_total = calc_annual_resource_total(data, rt)
        html += f"""    <div class="panel">
      <div class="panel-header" style="border-left:4px solid {rt_colors[ri]};">{rt} — Annual: {rt_total:,.0f} hours</div>
      <div class="panel-body">
        <table>
          <thead><tr><th>Project</th>"""
        for ms in MONTH_SHORT:
            html += f"<th>{ms}</th>"
        html += "<th>ANNUAL</th></tr></thead><tbody>\n"

        for p in projects:
            html += f"<tr><td>{p}</td>"
            p_rt_annual = 0
            for month in MONTHS:
                v = get_hours(data, month, p, rt)
                cls = "has-val" if v > 0 else "zero"
                html += f'<td class="{cls}">{v:,.0f}</td>'
                p_rt_annual += v
            html += f'<td class="total-col">{p_rt_annual:,.0f}</td></tr>\n'

        html += "</tbody><tfoot><tr><td>TOTAL</td>"
        for month in MONTHS:
            html += f"<td>{calc_monthly_resource_total(data, month, rt):,.0f}</td>"
        html += f'<td class="total-col">{rt_total:,.0f}</td></tr></tfoot></table>'
        html += """
      </div>
    </div>
"""

    html += """  </div>

  <!-- ═══════════════ TAB 5: YTD RUNNING TOTAL ═══════════════ -->
  <div class="tab-content" id="tab-ytd">
    <div class="panel">
      <div class="panel-header">Year-to-Date Cumulative Hours by Project</div>
      <div class="panel-body">
        <table>
          <thead><tr><th>Project</th>
"""
    for ms in MONTH_SHORT:
        html += f"<th>{ms} YTD</th>"
    html += "</tr></thead><tbody>\n"

    for p in projects:
        html += f"<tr><td>{p}</td>"
        for mi in range(12):
            ytd = calc_ytd_project_total(data, p, mi)
            cls = "has-val" if ytd > 0 else "zero"
            html += f'<td class="{cls}">{ytd:,.0f}</td>'
        html += "</tr>\n"

    html += "</tbody><tfoot><tr><td>TOTAL</td>"
    for mi in range(12):
        html += f"<td>{calc_ytd_grand_total(data, mi):,.0f}</td>"
    html += "</tr></tfoot></table>"

    html += """
      </div>
    </div>

    <div class="panel">
      <div class="panel-header">Year-to-Date Cumulative Hours by Resource Type</div>
      <div class="panel-body">
        <table>
          <thead><tr><th>Resource Type</th>
"""
    for ms in MONTH_SHORT:
        html += f"<th>{ms} YTD</th>"
    html += "</tr></thead><tbody>\n"

    for rt in rtypes:
        html += f"<tr><td>{rt}</td>"
        for mi in range(12):
            ytd = calc_ytd_resource_total(data, rt, mi)
            cls = "has-val" if ytd > 0 else "zero"
            html += f'<td class="{cls}">{ytd:,.0f}</td>'
        html += "</tr>\n"

    html += "</tbody><tfoot><tr><td>TOTAL</td>"
    for mi in range(12):
        html += f"<td>{calc_ytd_grand_total(data, mi):,.0f}</td>"
    html += "</tr></tfoot></table>"

    html += """
      </div>
    </div>
  </div>

  <!-- ═══════════════ FOOTER ═══════════════ -->
  <div style="text-align:center; padding:1.5rem; font-size:0.8rem; color:var(--text-lighter);">
    Prepared for Product Owner &amp; P&amp;L Analysis &bull; Confidential &bull; """ + str(year) + """
  </div>

</div>

<!-- ═══════════════ TAB JS ═══════════════ -->
<script>
document.querySelectorAll('.tab').forEach(tab => {
  tab.addEventListener('click', () => {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    tab.classList.add('active');
    document.getElementById(tab.dataset.tab).classList.add('active');
  });
});
</script>

</body>
</html>"""

    with open(output_path, "w") as f:
        f.write(html)

    print(f"  Full-year HTML dashboard saved to: {output_path}")


# ─────────────────────────────────────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────────────────────────────────────

def main():
    print("=" * 60)
    print("  ANNUAL MAN-HOURS REPORT GENERATOR")
    print("=" * 60)
    print()

    # Load data
    print(f"Loading data from: {DATA_FILE}")
    data = load_data()
    year = data["year"]
    projects = data["projects"]
    rtypes = data["resource_types"]

    print(f"Year: {year}")
    print(f"Projects: {', '.join(projects)}")
    print(f"Resource Types: {', '.join(rtypes)}")
    print()

    # Create output directory
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # 1. Full-year master CSV
    print("[1/3] Full-year master CSV (for Excel):")
    generate_full_year_csv(data, os.path.join(OUTPUT_DIR, f"annual_man_hours_{year}.csv"))

    # 2. Flat/pivot-ready CSV
    print("[2/3] Flat CSV (for Pivot Tables / Power BI):")
    generate_flat_csv(data, os.path.join(OUTPUT_DIR, f"annual_man_hours_{year}_flat.csv"))

    # 3. Full-year HTML dashboard
    print("[3/3] HTML annual dashboard:")
    generate_yearly_html(data, os.path.join(OUTPUT_DIR, f"annual_dashboard_{year}.html"))

    print()
    print("=" * 60)
    print("  ALL DONE!")
    print("=" * 60)
    print()
    print("HOW TO USE:")
    print()
    print("  STEP 1: Edit 'yearly_man_hours_data.json'")
    print("          Fill in actual hours for each month as data comes in.")
    print()
    print("  STEP 2: Run this script again:")
    print("          python3 generate_yearly_report.py")
    print()
    print("  STEP 3: Share with Product Owner:")
    print(f"          - HTML dashboard : reports/annual_dashboard_{year}.html")
    print(f"          - Excel CSV      : reports/annual_man_hours_{year}.csv")
    print(f"          - Pivot-ready CSV: reports/annual_man_hours_{year}_flat.csv")
    print()
    print("  Each month, just update the JSON and re-run. That's it!")
    print()


if __name__ == "__main__":
    main()
