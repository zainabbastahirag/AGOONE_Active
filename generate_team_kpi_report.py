#!/usr/bin/env python3
"""
Team KPI & Manpower Tracker — Report Generator
=================================================
Generates a professional team performance dashboard for a Tech Manager
tracking 7+ teams across multiple projects.

Tracks per person:
  - KPIs (tasks, bugs, quality, delivery, hours)
  - Errors log
  - Achievements
  - Tech Lead feedback
  - Manager feedback
  - Multi-project assignments
  - Extra hours / overtime

Usage:
    1. Edit team_kpi_data.json with actual data
    2. Run: python3 generate_team_kpi_report.py
    3. Open reports/team_kpi_dashboard_<month>_<year>.html in browser
"""

import csv
import json
import os
from datetime import datetime
from collections import defaultdict

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DATA_FILE = os.path.join(SCRIPT_DIR, "team_kpi_data.json")
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "reports")

KPI_FIELDS = [
    "tasks_assigned",
    "tasks_completed",
    "bugs_found_in_work",
    "bugs_fixed",
    "code_reviews_done",
    "on_time_delivery_pct",
    "quality_score",
    "extra_hours",
    "total_hours_worked",
]

KPI_LABELS = {
    "tasks_assigned": "Tasks Assigned",
    "tasks_completed": "Tasks Completed",
    "bugs_found_in_work": "Bugs in Work",
    "bugs_fixed": "Bugs Fixed",
    "code_reviews_done": "Code Reviews",
    "on_time_delivery_pct": "On-Time %",
    "quality_score": "Quality (1-5)",
    "extra_hours": "Extra Hours",
    "total_hours_worked": "Total Hours",
}


def load_data():
    with open(DATA_FILE, "r") as f:
        return json.load(f)


def all_members_flat(data):
    """Return a flat list of (team_name, member_name, member_data) across all teams."""
    result = []
    for team_name, team in data["teams"].items():
        for member_name, member in team["members"].items():
            result.append((team_name, team["project"], team["tech_lead"], member_name, member))
    return result


def unique_people(data):
    """Get unique person names (some appear in multiple teams)."""
    seen = {}
    for team_name, team in data["teams"].items():
        for member_name, member in team["members"].items():
            if member_name not in seen:
                seen[member_name] = []
            seen[member_name].append(team_name)
    return seen


# ─────────────────────────────────────────────────────────────────────────────
# CSV OUTPUTS
# ─────────────────────────────────────────────────────────────────────────────

def generate_master_csv(data, output_path):
    """One-row-per-person CSV with all KPIs, feedback, errors — Excel friendly."""
    with open(output_path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow([
            f"Team KPI Report — {data['report_month']} {data['report_year']}"
        ])
        w.writerow([f"Tech Manager: {data['tech_manager']}"])
        w.writerow([f"Generated: {datetime.now().strftime('%B %d, %Y %H:%M')}"])
        w.writerow([])

        header = [
            "Team", "Project", "Tech Lead", "Member", "Role",
            "Projects Assigned",
        ] + [KPI_LABELS[k] for k in KPI_FIELDS] + [
            "Completion Rate %",
            "Bug Ratio",
            "Achievements",
            "Errors Log",
            "Tech Lead Feedback",
            "Manager Feedback",
            "Progress Notes",
        ]
        w.writerow(header)

        for team_name, project, tech_lead, name, m in all_members_flat(data):
            kpi = m.get("kpi", {})
            assigned = kpi.get("tasks_assigned", 0)
            completed = kpi.get("tasks_completed", 0)
            bugs_in = kpi.get("bugs_found_in_work", 0)
            completion_rate = round((completed / assigned * 100), 1) if assigned > 0 else 0
            bug_ratio = round((bugs_in / completed), 2) if completed > 0 else 0

            row = [
                team_name,
                project,
                tech_lead,
                name,
                m.get("role", ""),
                " | ".join(m.get("projects_assigned", [])),
            ]
            for k in KPI_FIELDS:
                row.append(kpi.get(k, 0))
            row += [
                completion_rate,
                bug_ratio,
                m.get("achievements", ""),
                m.get("errors_log", ""),
                m.get("tech_lead_feedback", ""),
                m.get("manager_feedback", ""),
                m.get("progress_notes", ""),
            ]
            w.writerow(row)

    print(f"  Master CSV: {output_path}")


def generate_input_template_csv(data, output_path):
    """Blank template for Tech Leads to fill in for their team members."""
    with open(output_path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow([
            "Team", "Project", "Member", "Role",
            "Tasks Assigned", "Tasks Completed",
            "Bugs Found in Work", "Bugs Fixed",
            "Code Reviews Done", "On-Time Delivery %",
            "Quality Score (1-5)", "Extra Hours", "Total Hours Worked",
            "Achievements", "Errors Log",
            "Tech Lead Feedback", "Progress Notes",
        ])
        for team_name, project, tech_lead, name, m in all_members_flat(data):
            w.writerow([
                team_name, project, name, m.get("role", ""),
                "", "", "", "", "", "", "", "", "",
                "", "", "", "",
            ])

    print(f"  Input template CSV: {output_path}")


def generate_flat_csv(data, output_path):
    """Flat/normalized CSV for pivot tables and Power BI."""
    with open(output_path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow([
            "Year", "Month", "Team", "Project", "Tech Lead",
            "Member", "Role", "KPI Metric", "Value",
        ])
        for team_name, project, tech_lead, name, m in all_members_flat(data):
            kpi = m.get("kpi", {})
            for k in KPI_FIELDS:
                w.writerow([
                    data["report_year"],
                    data["report_month"],
                    team_name,
                    project,
                    tech_lead,
                    name,
                    m.get("role", ""),
                    KPI_LABELS[k],
                    kpi.get(k, 0),
                ])

    print(f"  Flat/pivot CSV: {output_path}")


# ─────────────────────────────────────────────────────────────────────────────
# HTML DASHBOARD
# ─────────────────────────────────────────────────────────────────────────────

def generate_html_dashboard(data, output_path):
    year = data["report_year"]
    month = data["report_month"]
    manager = data["tech_manager"]
    teams = data["teams"]
    projects = data["projects"]

    all_flat = all_members_flat(data)
    unique = unique_people(data)

    total_people = len(unique)
    total_teams = len(teams)

    # Aggregate KPIs
    sum_assigned = sum(m.get("kpi", {}).get("tasks_assigned", 0) for *_, m in all_flat)
    sum_completed = sum(m.get("kpi", {}).get("tasks_completed", 0) for *_, m in all_flat)
    sum_bugs_in = sum(m.get("kpi", {}).get("bugs_found_in_work", 0) for *_, m in all_flat)
    sum_bugs_fixed = sum(m.get("kpi", {}).get("bugs_fixed", 0) for *_, m in all_flat)
    sum_hours = sum(m.get("kpi", {}).get("total_hours_worked", 0) for *_, m in all_flat)
    sum_extra = sum(m.get("kpi", {}).get("extra_hours", 0) for *_, m in all_flat)
    overall_completion = round(sum_completed / sum_assigned * 100, 1) if sum_assigned > 0 else 0

    quality_vals = [m.get("kpi", {}).get("quality_score", 0) for *_, m in all_flat if m.get("kpi", {}).get("quality_score", 0) > 0]
    avg_quality = round(sum(quality_vals) / len(quality_vals), 1) if quality_vals else 0

    # Team colors
    team_colors = ["#3182ce", "#e53e3e", "#38a169", "#d69e2e", "#805ad5", "#dd6b20", "#319795", "#d53f8c", "#667eea"]

    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Team KPI Dashboard — {month} {year}</title>
<style>
:root {{
  --primary: #1a365d; --primary-l: #2b6cb0; --accent: #3182ce;
  --bg: #f0f4f8; --card: #fff; --border: #e2e8f0;
  --text: #2d3748; --text-l: #718096; --text-ll: #a0aec0;
  --green: #38a169; --red: #e53e3e; --yellow: #d69e2e;
  --purple: #805ad5; --orange: #dd6b20; --teal: #319795;
  --r: 10px;
  --sh: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);
  --sh-lg: 0 10px 15px -3px rgba(0,0,0,0.07);
}}
* {{ margin:0; padding:0; box-sizing:border-box; }}
body {{ font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif; background:var(--bg); color:var(--text); line-height:1.5; }}

.topbar {{
  background:linear-gradient(135deg,var(--primary) 0%,var(--primary-l) 100%);
  color:#fff; padding:1.25rem 2rem; display:flex; justify-content:space-between; align-items:center;
  position:sticky; top:0; z-index:100; box-shadow:var(--sh-lg);
}}
.topbar h1 {{ font-size:1.3rem; font-weight:700; }}
.topbar .sub {{ font-size:.78rem; opacity:.8; margin-top:.15rem; }}
.topbar .meta {{ text-align:right; font-size:.78rem; opacity:.85; }}
.topbar .badge {{ display:inline-block; background:rgba(255,255,255,.2); padding:.15rem .65rem; border-radius:20px; font-weight:600; font-size:.85rem; margin-bottom:.2rem; }}

.container {{ max-width:1400px; margin:0 auto; padding:1.25rem; }}

/* KPI row */
.kpis {{ display:grid; grid-template-columns:repeat(auto-fit,minmax(170px,1fr)); gap:.75rem; margin-bottom:1.25rem; }}
.kpi {{ background:var(--card); border-radius:var(--r); padding:1rem 1.15rem; box-shadow:var(--sh); border-left:4px solid var(--accent); }}
.kpi.g {{ border-left-color:var(--green); }} .kpi.r {{ border-left-color:var(--red); }}
.kpi.y {{ border-left-color:var(--yellow); }} .kpi.p {{ border-left-color:var(--purple); }}
.kpi.o {{ border-left-color:var(--orange); }} .kpi.t {{ border-left-color:var(--teal); }}
.kpi .kl {{ font-size:.65rem; text-transform:uppercase; letter-spacing:.07em; color:var(--text-l); margin-bottom:.15rem; }}
.kpi .kv {{ font-size:1.5rem; font-weight:800; color:var(--primary); line-height:1.2; }}
.kpi .ks {{ font-size:.7rem; color:var(--text-ll); margin-top:.1rem; }}

/* Tabs */
.tabs {{ display:flex; gap:0; border-bottom:2px solid var(--border); margin-bottom:1.25rem; overflow-x:auto; }}
.tab {{ padding:.65rem 1.15rem; font-size:.8rem; font-weight:600; color:var(--text-l); cursor:pointer;
  border-bottom:3px solid transparent; transition:all .2s; white-space:nowrap; user-select:none; }}
.tab:hover {{ color:var(--primary); background:rgba(49,130,206,.04); }}
.tab.active {{ color:var(--accent); border-bottom-color:var(--accent); }}
.tc {{ display:none; }} .tc.active {{ display:block; }}

/* Panel */
.pnl {{ background:var(--card); border-radius:var(--r); box-shadow:var(--sh); margin-bottom:1.25rem; overflow:hidden; }}
.pnl-h {{ padding:.85rem 1.25rem; font-weight:700; font-size:.9rem; color:var(--primary);
  border-bottom:1px solid var(--border); background:#f8fafc; display:flex; justify-content:space-between; align-items:center; }}
.pnl-h .tag {{ font-size:.7rem; padding:.15rem .5rem; border-radius:12px; color:#fff; font-weight:600; }}
.pnl-b {{ padding:1rem 1.25rem; overflow-x:auto; }}

/* Tables */
table {{ width:100%; border-collapse:collapse; font-size:.8rem; }}
th {{ background:var(--primary); color:#fff; padding:.6rem .65rem; text-align:center;
  font-size:.7rem; text-transform:uppercase; letter-spacing:.04em; font-weight:600; white-space:nowrap; }}
th:first-child {{ text-align:left; }}
td {{ padding:.55rem .65rem; text-align:center; border-bottom:1px solid var(--border); white-space:nowrap; }}
td:first-child {{ text-align:left; font-weight:600; color:var(--primary); }}
tbody tr:hover {{ background:#edf2f7; }}
tbody tr:nth-child(even) {{ background:#fafbfc; }}
tbody tr:nth-child(even):hover {{ background:#edf2f7; }}
tfoot td {{ font-weight:700; background:#edf2f7; border-top:2px solid var(--primary); }}
.zero {{ color:var(--text-ll); }}
.good {{ color:var(--green); font-weight:700; }}
.warn {{ color:var(--yellow); font-weight:700; }}
.bad {{ color:var(--red); font-weight:700; }}

/* Feedback cards */
.fb-grid {{ display:grid; grid-template-columns:repeat(auto-fill,minmax(320px,1fr)); gap:1rem; }}
.fb-card {{ background:var(--card); border-radius:var(--r); box-shadow:var(--sh); padding:1.15rem; border-left:4px solid var(--accent); }}
.fb-card .fb-name {{ font-weight:700; color:var(--primary); font-size:.9rem; }}
.fb-card .fb-role {{ font-size:.7rem; color:var(--text-l); margin-bottom:.5rem; }}
.fb-card .fb-section {{ margin-bottom:.6rem; }}
.fb-card .fb-label {{ font-size:.65rem; text-transform:uppercase; letter-spacing:.06em; color:var(--text-l); font-weight:600; margin-bottom:.1rem; }}
.fb-card .fb-text {{ font-size:.8rem; color:var(--text); background:#f7fafc; padding:.4rem .6rem; border-radius:6px; border:1px solid var(--border); min-height:1.8rem; }}
.fb-card .fb-text.empty {{ color:var(--text-ll); font-style:italic; }}

/* Multi-project badge */
.mp-badge {{ display:inline-block; padding:.1rem .4rem; border-radius:10px; font-size:.65rem; font-weight:600; margin:.1rem; }}

/* Progress bar */
.prog {{ display:flex; align-items:center; gap:.5rem; }}
.prog-track {{ flex:1; height:8px; background:#edf2f7; border-radius:4px; overflow:hidden; }}
.prog-fill {{ height:100%; border-radius:4px; transition:width .3s; }}
.prog-val {{ font-size:.75rem; font-weight:700; min-width:40px; text-align:right; }}

/* Quality stars */
.stars {{ font-size:1rem; letter-spacing:2px; }}

@media print {{
  body {{ background:#fff; }} .topbar {{ position:static; }}
  .tabs {{ display:none; }} .tc {{ display:block !important; page-break-inside:avoid; }}
  .pnl {{ box-shadow:none; border:1px solid #ddd; }}
}}
@media (max-width:768px) {{
  .container {{ padding:.5rem; }} .kpis {{ grid-template-columns:repeat(2,1fr); }}
  .topbar {{ flex-direction:column; text-align:center; gap:.4rem; }}
}}
</style>
</head>
<body>

<div class="topbar">
  <div>
    <h1>Team KPI & Manpower Tracker</h1>
    <div class="sub">Tech Manager: {manager} &bull; {total_teams} Teams &bull; {total_people} People &bull; {len(projects)} Projects</div>
  </div>
  <div class="meta">
    <div class="badge">{month} {year}</div>
    <div>Generated: {datetime.now().strftime("%b %d, %Y %H:%M")}</div>
  </div>
</div>

<div class="container">

<!-- ══════ KPI CARDS ══════ -->
<div class="kpis">
  <div class="kpi g"><div class="kl">Total People</div><div class="kv">{total_people}</div><div class="ks">across {total_teams} teams</div></div>
  <div class="kpi"><div class="kl">Tasks Assigned</div><div class="kv">{sum_assigned}</div><div class="ks">this month</div></div>
  <div class="kpi g"><div class="kl">Tasks Completed</div><div class="kv">{sum_completed}</div><div class="ks">{overall_completion}% completion</div></div>
  <div class="kpi r"><div class="kl">Bugs Found</div><div class="kv">{sum_bugs_in}</div><div class="ks">in delivered work</div></div>
  <div class="kpi y"><div class="kl">Bugs Fixed</div><div class="kv">{sum_bugs_fixed}</div><div class="ks">resolved this month</div></div>
  <div class="kpi p"><div class="kl">Avg Quality</div><div class="kv">{avg_quality}/5</div><div class="ks">team average</div></div>
  <div class="kpi t"><div class="kl">Total Hours</div><div class="kv">{sum_hours:,.0f}</div><div class="ks">man-hours worked</div></div>
  <div class="kpi o"><div class="kl">Extra Hours</div><div class="kv">{sum_extra:,.0f}</div><div class="ks">overtime</div></div>
</div>

<!-- ══════ TABS ══════ -->
<div class="tabs" id="mainTabs">
  <div class="tab active" data-tab="t-all">All Members</div>
  <div class="tab" data-tab="t-teams">By Team</div>
  <div class="tab" data-tab="t-kpi">KPI Scorecard</div>
  <div class="tab" data-tab="t-errors">Errors & Bugs</div>
  <div class="tab" data-tab="t-feedback">Feedback & Achievements</div>
  <div class="tab" data-tab="t-hours">Hours & Overtime</div>
  <div class="tab" data-tab="t-multi">Multi-Project Members</div>
</div>

"""

    # ════════════════════════════════════════════════════════════════
    # TAB 1: ALL MEMBERS (master table)
    # ════════════════════════════════════════════════════════════════
    html += """<div class="tc active" id="t-all">
<div class="pnl"><div class="pnl-h">All Team Members — Complete Overview</div><div class="pnl-b">
<table><thead><tr>
  <th>Name</th><th>Team</th><th>Project</th><th>Role</th>
  <th>Assigned</th><th>Completed</th><th>Completion</th>
  <th>Bugs In</th><th>Bugs Fixed</th><th>Reviews</th>
  <th>On-Time %</th><th>Quality</th><th>Hours</th><th>Extra Hrs</th>
</tr></thead><tbody>
"""
    for team_name, project, tech_lead, name, m in all_flat:
        kpi = m.get("kpi", {})
        assigned = kpi.get("tasks_assigned", 0)
        completed = kpi.get("tasks_completed", 0)
        comp_pct = round(completed / assigned * 100) if assigned > 0 else 0
        bugs_in = kpi.get("bugs_found_in_work", 0)
        bugs_fixed = kpi.get("bugs_fixed", 0)
        reviews = kpi.get("code_reviews_done", 0)
        ontime = kpi.get("on_time_delivery_pct", 0)
        quality = kpi.get("quality_score", 0)
        hours = kpi.get("total_hours_worked", 0)
        extra = kpi.get("extra_hours", 0)
        role = m.get("role", "")

        comp_cls = "good" if comp_pct >= 80 else ("warn" if comp_pct >= 50 else ("bad" if assigned > 0 else "zero"))
        ontime_cls = "good" if ontime >= 80 else ("warn" if ontime >= 50 else ("bad" if ontime > 0 else "zero"))
        q_cls = "good" if quality >= 4 else ("warn" if quality >= 3 else ("bad" if quality > 0 else "zero"))
        bug_cls = "bad" if bugs_in > 3 else ("warn" if bugs_in > 0 else "zero")

        role_icon = "&#9733; " if "Lead" in role else ""

        html += f"""<tr>
  <td>{role_icon}{name}</td><td>{team_name}</td><td>{project}</td><td>{role}</td>
  <td class="{'has-val' if assigned else 'zero'}">{assigned}</td>
  <td class="{'has-val' if completed else 'zero'}">{completed}</td>
  <td class="{comp_cls}">{comp_pct}%</td>
  <td class="{bug_cls}">{bugs_in}</td>
  <td class="{'good' if bugs_fixed else 'zero'}">{bugs_fixed}</td>
  <td class="{'has-val' if reviews else 'zero'}">{reviews}</td>
  <td class="{ontime_cls}">{ontime}%</td>
  <td class="{q_cls}">{quality}</td>
  <td class="{'has-val' if hours else 'zero'}">{hours}</td>
  <td class="{'warn' if extra > 0 else 'zero'}">{extra}</td>
</tr>
"""

    html += """</tbody></table>
</div></div></div>
"""

    # ════════════════════════════════════════════════════════════════
    # TAB 2: BY TEAM
    # ════════════════════════════════════════════════════════════════
    html += '<div class="tc" id="t-teams">\n'

    for ti, (team_name, team) in enumerate(teams.items()):
        color = team_colors[ti % len(team_colors)]
        members = team["members"]
        t_assigned = sum(m.get("kpi", {}).get("tasks_assigned", 0) for m in members.values())
        t_completed = sum(m.get("kpi", {}).get("tasks_completed", 0) for m in members.values())
        t_hours = sum(m.get("kpi", {}).get("total_hours_worked", 0) for m in members.values())
        t_comp_pct = round(t_completed / t_assigned * 100) if t_assigned > 0 else 0

        html += f"""<div class="pnl">
<div class="pnl-h" style="border-left:4px solid {color};">
  {team_name} — {team["project"]}
  <span><span class="tag" style="background:{color};">Lead: {team["tech_lead"]}</span>
  &nbsp; {len(members)} members &bull; {t_hours} hrs &bull; {t_comp_pct}% completion</span>
</div>
<div class="pnl-b"><table><thead><tr>
  <th>Member</th><th>Role</th><th>Assigned</th><th>Completed</th><th>Completion</th>
  <th>Bugs In</th><th>Bugs Fixed</th><th>On-Time %</th><th>Quality</th><th>Hours</th><th>Extra</th>
</tr></thead><tbody>
"""
        for name, m in members.items():
            kpi = m.get("kpi", {})
            a = kpi.get("tasks_assigned", 0)
            c = kpi.get("tasks_completed", 0)
            cp = round(c / a * 100) if a > 0 else 0
            bi = kpi.get("bugs_found_in_work", 0)
            bf = kpi.get("bugs_fixed", 0)
            ot = kpi.get("on_time_delivery_pct", 0)
            q = kpi.get("quality_score", 0)
            h = kpi.get("total_hours_worked", 0)
            ex = kpi.get("extra_hours", 0)
            role_icon = "&#9733; " if "Lead" in m.get("role", "") else ""

            html += f"""<tr>
  <td>{role_icon}{name}</td><td>{m.get("role","")}</td>
  <td>{a}</td><td>{c}</td><td>{cp}%</td>
  <td>{bi}</td><td>{bf}</td><td>{ot}%</td><td>{q}</td><td>{h}</td><td>{ex}</td>
</tr>
"""

        t_bugs_in = sum(m.get("kpi", {}).get("bugs_found_in_work", 0) for m in members.values())
        t_bugs_fixed = sum(m.get("kpi", {}).get("bugs_fixed", 0) for m in members.values())
        t_extra = sum(m.get("kpi", {}).get("extra_hours", 0) for m in members.values())

        html += f"""</tbody><tfoot><tr>
  <td>TEAM TOTAL</td><td></td><td>{t_assigned}</td><td>{t_completed}</td><td>{t_comp_pct}%</td>
  <td>{t_bugs_in}</td>
  <td>{t_bugs_fixed}</td>
  <td></td><td></td><td>{t_hours}</td>
  <td>{t_extra}</td>
</tr></tfoot></table></div></div>
"""

    html += '</div>\n'

    # ════════════════════════════════════════════════════════════════
    # TAB 3: KPI SCORECARD (visual)
    # ════════════════════════════════════════════════════════════════
    html += '<div class="tc" id="t-kpi">\n'
    html += '<div class="pnl"><div class="pnl-h">Individual KPI Scorecard</div><div class="pnl-b">\n'
    html += """<table><thead><tr>
  <th>Name</th><th>Team</th><th>Completion Rate</th><th>On-Time Delivery</th><th>Quality Score</th><th>Bug Ratio</th><th>Overall Grade</th>
</tr></thead><tbody>
"""

    for team_name, project, tech_lead, name, m in all_flat:
        kpi = m.get("kpi", {})
        a = kpi.get("tasks_assigned", 0)
        c = kpi.get("tasks_completed", 0)
        cp = round(c / a * 100) if a > 0 else 0
        ot = kpi.get("on_time_delivery_pct", 0)
        q = kpi.get("quality_score", 0)
        bi = kpi.get("bugs_found_in_work", 0)
        bug_ratio = round(bi / c, 2) if c > 0 else 0

        # Calculate overall grade (weighted average)
        if a > 0 or q > 0:
            grade_score = (cp * 0.3) + (ot * 0.3) + (q * 20 * 0.25) + ((1 - min(bug_ratio, 1)) * 100 * 0.15)
            if grade_score >= 85:
                grade, grade_cls = "A", "good"
            elif grade_score >= 70:
                grade, grade_cls = "B", "good"
            elif grade_score >= 55:
                grade, grade_cls = "C", "warn"
            elif grade_score >= 40:
                grade, grade_cls = "D", "warn"
            else:
                grade, grade_cls = "F", "bad"
        else:
            grade, grade_cls = "-", "zero"
            grade_score = 0

        stars = ""
        for si in range(5):
            stars += "&#9733;" if si < q else "&#9734;"

        comp_cls = "good" if cp >= 80 else ("warn" if cp >= 50 else ("bad" if a > 0 else "zero"))
        ot_cls = "good" if ot >= 80 else ("warn" if ot >= 50 else ("bad" if ot > 0 else "zero"))
        br_cls = "good" if bug_ratio == 0 and c > 0 else ("warn" if bug_ratio <= 0.2 else ("bad" if bug_ratio > 0.2 else "zero"))

        html += f"""<tr>
  <td>{name}</td><td>{team_name}</td>
  <td><div class="prog"><div class="prog-track"><div class="prog-fill" style="width:{cp}%;background:{'var(--green)' if cp>=80 else ('var(--yellow)' if cp>=50 else 'var(--red)')};"></div></div><div class="prog-val {comp_cls}">{cp}%</div></div></td>
  <td><div class="prog"><div class="prog-track"><div class="prog-fill" style="width:{ot}%;background:{'var(--green)' if ot>=80 else ('var(--yellow)' if ot>=50 else 'var(--red)')};"></div></div><div class="prog-val {ot_cls}">{ot}%</div></div></td>
  <td class="stars">{stars}</td>
  <td class="{br_cls}">{bug_ratio}</td>
  <td class="{grade_cls}" style="font-size:1.1rem;font-weight:800;">{grade}</td>
</tr>
"""

    html += '</tbody></table></div></div>\n'

    # KPI legend
    html += """<div class="pnl"><div class="pnl-h">KPI Definitions & Grading</div><div class="pnl-b">
<table><thead><tr><th>KPI Metric</th><th>Description</th><th>Weight in Grade</th></tr></thead><tbody>
<tr><td>Completion Rate</td><td>Tasks Completed / Tasks Assigned x 100</td><td>30%</td></tr>
<tr><td>On-Time Delivery</td><td>Percentage of tasks delivered on or before deadline</td><td>30%</td></tr>
<tr><td>Quality Score</td><td>Tech Lead rating: 1=Poor, 2=Below Avg, 3=Average, 4=Good, 5=Excellent</td><td>25%</td></tr>
<tr><td>Bug Ratio</td><td>Bugs found in delivered work / Tasks completed (lower is better)</td><td>15%</td></tr>
</tbody></table>
<div style="margin-top:.75rem;font-size:.8rem;color:var(--text-l);">
  <strong>Grading:</strong> A = 85%+ &bull; B = 70-84% &bull; C = 55-69% &bull; D = 40-54% &bull; F = below 40%
</div>
</div></div>
"""
    html += '</div>\n'

    # ════════════════════════════════════════════════════════════════
    # TAB 4: ERRORS & BUGS
    # ════════════════════════════════════════════════════════════════
    html += '<div class="tc" id="t-errors">\n'
    html += '<div class="pnl"><div class="pnl-h">Error & Bug Tracking by Person</div><div class="pnl-b">\n'
    html += """<table><thead><tr>
  <th>Name</th><th>Team</th><th>Project</th><th>Bugs in Work</th><th>Bugs Fixed</th><th>Net Open</th><th>Bug Ratio</th><th>Errors Log</th>
</tr></thead><tbody>
"""

    for team_name, project, tech_lead, name, m in all_flat:
        kpi = m.get("kpi", {})
        bi = kpi.get("bugs_found_in_work", 0)
        bf = kpi.get("bugs_fixed", 0)
        c = kpi.get("tasks_completed", 0)
        net = bi - bf
        br = round(bi / c, 2) if c > 0 else 0
        err = m.get("errors_log", "")

        net_cls = "bad" if net > 0 else ("good" if net < 0 else "zero")
        bi_cls = "bad" if bi > 3 else ("warn" if bi > 0 else "zero")
        br_cls = "bad" if br > 0.2 else ("warn" if br > 0 else "zero")

        err_display = err if err else '<span class="zero">—</span>'

        html += f"""<tr>
  <td>{name}</td><td>{team_name}</td><td>{project}</td>
  <td class="{bi_cls}">{bi}</td><td class="{'good' if bf else 'zero'}">{bf}</td>
  <td class="{net_cls}">{net}</td><td class="{br_cls}">{br}</td>
  <td style="text-align:left;white-space:normal;max-width:250px;font-size:.75rem;">{err_display}</td>
</tr>
"""

    html += '</tbody></table></div></div></div>\n'

    # ════════════════════════════════════════════════════════════════
    # TAB 5: FEEDBACK & ACHIEVEMENTS
    # ════════════════════════════════════════════════════════════════
    html += '<div class="tc" id="t-feedback">\n'
    html += '<div class="fb-grid">\n'

    for ti, (team_name, project, tech_lead, name, m) in enumerate(all_flat):
        color = team_colors[ti % len(team_colors)]
        achievements = m.get("achievements", "")
        tl_fb = m.get("tech_lead_feedback", "")
        mgr_fb = m.get("manager_feedback", "")
        progress = m.get("progress_notes", "")
        role = m.get("role", "")
        projs = ", ".join(m.get("projects_assigned", []))

        html += f"""<div class="fb-card" style="border-left-color:{color};">
  <div class="fb-name">{name}</div>
  <div class="fb-role">{role} &bull; {team_name} &bull; {projs}</div>
  <div class="fb-section"><div class="fb-label">Achievements</div><div class="fb-text {'empty' if not achievements else ''}">{achievements or 'No achievements recorded'}</div></div>
  <div class="fb-section"><div class="fb-label">Tech Lead Feedback</div><div class="fb-text {'empty' if not tl_fb else ''}">{tl_fb or 'No feedback yet'}</div></div>
  <div class="fb-section"><div class="fb-label">Manager Feedback</div><div class="fb-text {'empty' if not mgr_fb else ''}">{mgr_fb or 'No feedback yet'}</div></div>
  <div class="fb-section"><div class="fb-label">Progress Notes</div><div class="fb-text {'empty' if not progress else ''}">{progress or 'No notes'}</div></div>
</div>
"""

    html += '</div></div>\n'

    # ════════════════════════════════════════════════════════════════
    # TAB 6: HOURS & OVERTIME
    # ════════════════════════════════════════════════════════════════
    html += '<div class="tc" id="t-hours">\n'
    html += '<div class="pnl"><div class="pnl-h">Hours Worked & Overtime Tracking</div><div class="pnl-b">\n'
    html += """<table><thead><tr>
  <th>Name</th><th>Team</th><th>Project(s)</th><th>Total Hours</th><th>Extra Hours</th><th>Overtime %</th><th>Hours Breakdown</th>
</tr></thead><tbody>
"""

    for team_name, project, tech_lead, name, m in all_flat:
        kpi = m.get("kpi", {})
        h = kpi.get("total_hours_worked", 0)
        ex = kpi.get("extra_hours", 0)
        ot_pct = round(ex / h * 100) if h > 0 else 0
        projs = ", ".join(m.get("projects_assigned", []))
        regular = h - ex

        bar_reg_pct = round(regular / max(h, 1) * 100)
        bar_ex_pct = 100 - bar_reg_pct if h > 0 else 0
        ot_cls = "bad" if ot_pct > 20 else ("warn" if ot_pct > 10 else ("good" if h > 0 else "zero"))

        html += f"""<tr>
  <td>{name}</td><td>{team_name}</td><td style="font-size:.75rem;">{projs}</td>
  <td class="{'has-val' if h else 'zero'}" style="font-weight:700;">{h}</td>
  <td class="{'warn' if ex > 0 else 'zero'}" style="font-weight:700;">{ex}</td>
  <td class="{ot_cls}">{ot_pct}%</td>
  <td style="min-width:160px;"><div class="prog"><div class="prog-track" style="height:14px;">
    <div class="prog-fill" style="width:{bar_reg_pct}%;background:var(--accent);border-radius:4px 0 0 4px;"></div>
    <div class="prog-fill" style="width:{bar_ex_pct}%;background:var(--orange);border-radius:0 4px 4px 0;position:absolute;left:{bar_reg_pct}%;"></div>
  </div></div></td>
</tr>
"""

    html += f"""</tbody><tfoot><tr>
  <td>TOTAL</td><td></td><td></td>
  <td style="font-weight:800;">{sum_hours}</td><td style="font-weight:800;">{sum_extra}</td>
  <td>{round(sum_extra/sum_hours*100) if sum_hours>0 else 0}%</td><td></td>
</tr></tfoot></table></div></div></div>
"""

    # ════════════════════════════════════════════════════════════════
    # TAB 7: MULTI-PROJECT MEMBERS
    # ════════════════════════════════════════════════════════════════
    html += '<div class="tc" id="t-multi">\n'
    html += '<div class="pnl"><div class="pnl-h">Members Working Across Multiple Projects</div><div class="pnl-b">\n'

    multi = {name: teams_list for name, teams_list in unique.items() if len(teams_list) > 1}

    if multi:
        html += """<table><thead><tr><th>Name</th><th>Teams</th><th>Projects</th><th>Total Hours (Combined)</th></tr></thead><tbody>\n"""
        for name, teams_list in multi.items():
            proj_set = set()
            combined_hours = 0
            for team_name, project, tech_lead, mname, m in all_flat:
                if mname == name:
                    proj_set.update(m.get("projects_assigned", []))
                    combined_hours += m.get("kpi", {}).get("total_hours_worked", 0)
            projs_html = " ".join(f'<span class="mp-badge" style="background:{team_colors[i % len(team_colors)]}">{p}</span>' for i, p in enumerate(proj_set))
            teams_html = ", ".join(teams_list)
            html += f'<tr><td style="font-weight:700;">{name}</td><td>{teams_html}</td><td>{projs_html}</td><td>{combined_hours}</td></tr>\n'
        html += '</tbody></table>\n'
    else:
        html += '<div style="padding:2rem;text-align:center;color:var(--text-l);">No members currently assigned to multiple projects.</div>\n'

    html += '</div></div>\n'

    # Note about multi-project
    html += """<div class="pnl"><div class="pnl-h">About Multi-Project Tracking</div><div class="pnl-b" style="font-size:.85rem;color:var(--text-l);">
  <p>Members who work on more than one project appear in multiple teams. Their KPIs are tracked separately per team/project, so you can see exactly how their effort is split. The combined hours above reflect their total contribution across all projects.</p>
</div></div>
"""

    html += '</div>\n'

    # ════════════════════════════════════════════════════════════════
    # FOOTER + JS
    # ════════════════════════════════════════════════════════════════
    html += f"""
<div style="text-align:center;padding:1.5rem;font-size:.75rem;color:var(--text-ll);">
  Team KPI & Manpower Report &bull; {month} {year} &bull; Confidential
</div>

</div>

<script>
document.querySelectorAll('.tab').forEach(tab => {{
  tab.addEventListener('click', () => {{
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tc').forEach(c => c.classList.remove('active'));
    tab.classList.add('active');
    document.getElementById(tab.dataset.tab).classList.add('active');
  }});
}});
</script>

</body>
</html>"""

    with open(output_path, "w", encoding="utf-8") as f:
        f.write(html)

    print(f"  HTML Dashboard: {output_path}")


# ─────────────────────────────────────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────────────────────────────────────

def main():
    print("=" * 60)
    print("  TEAM KPI & MANPOWER REPORT GENERATOR")
    print("=" * 60)
    print()

    data = load_data()
    year = data["report_year"]
    month = data["report_month"]
    manager = data["tech_manager"]
    teams = data["teams"]

    unique = unique_people(data)
    multi = {n: t for n, t in unique.items() if len(t) > 1}

    print(f"  Manager     : {manager}")
    print(f"  Period      : {month} {year}")
    print(f"  Teams       : {len(teams)}")
    print(f"  People      : {len(unique)} unique ({len(multi)} multi-project)")
    print(f"  Projects    : {', '.join(data['projects'])}")
    print()

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    slug = f"{month.lower()}_{year}"

    # 1. Master CSV
    print("[1/4] Master CSV (all KPIs + feedback):")
    generate_master_csv(data, os.path.join(OUTPUT_DIR, f"team_kpi_master_{slug}.csv"))

    # 2. Input template
    print("[2/4] Input template (for Tech Leads):")
    generate_input_template_csv(data, os.path.join(OUTPUT_DIR, f"team_kpi_input_template_{slug}.csv"))

    # 3. Flat/pivot CSV
    print("[3/4] Flat CSV (for Pivot Tables):")
    generate_flat_csv(data, os.path.join(OUTPUT_DIR, f"team_kpi_flat_{slug}.csv"))

    # 4. HTML Dashboard
    print("[4/4] HTML Dashboard:")
    generate_html_dashboard(data, os.path.join(OUTPUT_DIR, f"team_kpi_dashboard_{slug}.html"))

    print()
    print("=" * 60)
    print("  DONE!")
    print("=" * 60)
    print()
    print("  HOW TO USE:")
    print()
    print("  1. Edit  team_kpi_data.json  (fill in KPIs, feedback, errors)")
    print("  2. Run   python3 generate_team_kpi_report.py")
    print(f"  3. Open  reports/team_kpi_dashboard_{slug}.html  in browser")
    print()
    print("  SHARE WITH TECH LEADS:")
    print(f"  - Template: reports/team_kpi_input_template_{slug}.csv")
    print("    (They fill it in, you transfer data to the JSON)")
    print()
    print("  EACH MONTH:")
    print("  - Change 'report_month' in team_kpi_data.json")
    print("  - Update KPI numbers, feedback, errors, achievements")
    print("  - Re-run the script")
    print()


if __name__ == "__main__":
    main()
