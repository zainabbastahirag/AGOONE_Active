#!/usr/bin/env python3
"""
Monthly Man-Hours Report Generator
===================================
Generates a professional monthly man-hours report for P&L analysis.

Products: AGONEWorkj, OJE Safe, ONe Learn, Ne Pulse, AGONE
Resource Types: Product, UI/UX, Dev, QA

Author: Technical Manager Report Tool
Reporting Period: Starting January 2026
"""

import csv
import json
import os
from datetime import datetime, date
from collections import defaultdict

# ─────────────────────────────────────────────────────────────────────────────
# CONFIGURATION
# ─────────────────────────────────────────────────────────────────────────────

PROJECTS = [
    "AGONEWorkj",
    "OJE Safe",
    "ONe Learn",
    "Ne Pulse",
    "AGONE",
]

RESOURCE_TYPES = [
    "Product",
    "UI/UX",
    "Dev",
    "QA",
]

REPORT_START_YEAR = 2026
REPORT_START_MONTH = 1  # January

# ─────────────────────────────────────────────────────────────────────────────
# DATA TEMPLATE (January 2026 — fill in actual hours)
# ─────────────────────────────────────────────────────────────────────────────

# Structure: { "Project Name": { "Resource Type": hours } }
# Replace 0.0 with actual validated man-hours from each team lead.

JANUARY_2026_DATA = {
    "AGONEWorkj": {
        "Product": 0.0,
        "UI/UX": 0.0,
        "Dev": 0.0,
        "QA": 0.0,
    },
    "OJE Safe": {
        "Product": 0.0,
        "UI/UX": 0.0,
        "Dev": 0.0,
        "QA": 0.0,
    },
    "ONe Learn": {
        "Product": 0.0,
        "UI/UX": 0.0,
        "Dev": 0.0,
        "QA": 0.0,
    },
    "Ne Pulse": {
        "Product": 0.0,
        "UI/UX": 0.0,
        "Dev": 0.0,
        "QA": 0.0,
    },
    "AGONE": {
        "Product": 0.0,
        "UI/UX": 0.0,
        "Dev": 0.0,
        "QA": 0.0,
    },
}

# ─────────────────────────────────────────────────────────────────────────────
# REPORT GENERATION
# ─────────────────────────────────────────────────────────────────────────────


def get_month_label(year: int, month: int) -> str:
    """Return a human-readable month label like 'January 2026'."""
    return date(year, month, 1).strftime("%B %Y")


def compute_totals(data: dict) -> dict:
    """Compute per-project totals, per-resource-type totals, and grand total."""
    project_totals = {}
    resource_totals = defaultdict(float)
    grand_total = 0.0

    for project in PROJECTS:
        project_total = 0.0
        for rtype in RESOURCE_TYPES:
            hours = data.get(project, {}).get(rtype, 0.0)
            project_total += hours
            resource_totals[rtype] += hours
        project_totals[project] = project_total
        grand_total += project_total

    return {
        "project_totals": project_totals,
        "resource_totals": dict(resource_totals),
        "grand_total": grand_total,
    }


def generate_csv_report(data: dict, month_label: str, output_path: str):
    """Generate a CSV version of the man-hours report."""
    totals = compute_totals(data)

    with open(output_path, "w", newline="") as f:
        writer = csv.writer(f)

        # Header
        writer.writerow([f"Monthly Man-Hours Report — {month_label}"])
        writer.writerow([])
        writer.writerow(["Project"] + RESOURCE_TYPES + ["Total Hours"])
        writer.writerow([])

        # Data rows
        for project in PROJECTS:
            row = [project]
            for rtype in RESOURCE_TYPES:
                row.append(data.get(project, {}).get(rtype, 0.0))
            row.append(totals["project_totals"][project])
            writer.writerow(row)

        # Totals row
        writer.writerow([])
        total_row = ["TOTAL"]
        for rtype in RESOURCE_TYPES:
            total_row.append(totals["resource_totals"].get(rtype, 0.0))
        total_row.append(totals["grand_total"])
        writer.writerow(total_row)

    print(f"  CSV report saved to: {output_path}")


def generate_json_report(data: dict, month_label: str, output_path: str):
    """Generate a JSON version of the man-hours report for system integration."""
    totals = compute_totals(data)

    report = {
        "report_title": f"Monthly Man-Hours Report — {month_label}",
        "generated_at": datetime.now().isoformat(),
        "reporting_period": month_label,
        "projects": PROJECTS,
        "resource_types": RESOURCE_TYPES,
        "data": data,
        "summary": {
            "project_totals": totals["project_totals"],
            "resource_type_totals": totals["resource_totals"],
            "grand_total": totals["grand_total"],
        },
    }

    with open(output_path, "w") as f:
        json.dump(report, f, indent=2)

    print(f"  JSON report saved to: {output_path}")


def generate_html_report(data: dict, month_label: str, output_path: str):
    """Generate a professional HTML report with print-ready styling."""
    totals = compute_totals(data)

    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Monthly Man-Hours Report — {month_label}</title>
<style>
  :root {{
    --primary: #1a365d;
    --primary-light: #2b6cb0;
    --accent: #3182ce;
    --bg: #f7fafc;
    --card-bg: #ffffff;
    --border: #e2e8f0;
    --text: #2d3748;
    --text-light: #718096;
    --success: #38a169;
    --warning: #d69e2e;
  }}
  * {{ margin: 0; padding: 0; box-sizing: border-box; }}
  body {{
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, sans-serif;
    background: var(--bg);
    color: var(--text);
    line-height: 1.6;
    padding: 2rem;
  }}
  .container {{
    max-width: 1100px;
    margin: 0 auto;
  }}
  .header {{
    background: linear-gradient(135deg, var(--primary) 0%, var(--primary-light) 100%);
    color: white;
    padding: 2rem 2.5rem;
    border-radius: 12px 12px 0 0;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }}
  .header h1 {{
    font-size: 1.5rem;
    font-weight: 700;
    letter-spacing: -0.025em;
  }}
  .header .meta {{
    text-align: right;
    font-size: 0.875rem;
    opacity: 0.9;
  }}
  .header .meta .period {{
    font-size: 1.1rem;
    font-weight: 600;
    margin-bottom: 0.25rem;
  }}
  .card {{
    background: var(--card-bg);
    border: 1px solid var(--border);
    border-top: none;
    padding: 2rem 2.5rem;
    border-radius: 0 0 12px 12px;
    box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);
  }}
  .summary-grid {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 1rem;
    margin-bottom: 2rem;
  }}
  .summary-box {{
    background: var(--bg);
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 1rem 1.25rem;
    text-align: center;
  }}
  .summary-box .label {{
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-light);
    margin-bottom: 0.25rem;
  }}
  .summary-box .value {{
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--primary);
  }}
  .summary-box.grand .value {{
    color: var(--success);
    font-size: 1.75rem;
  }}
  table {{
    width: 100%;
    border-collapse: collapse;
    margin-top: 1rem;
  }}
  thead th {{
    background: var(--primary);
    color: white;
    padding: 0.875rem 1rem;
    text-align: center;
    font-size: 0.85rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    font-weight: 600;
  }}
  thead th:first-child {{
    text-align: left;
    border-radius: 8px 0 0 0;
  }}
  thead th:last-child {{
    border-radius: 0 8px 0 0;
  }}
  tbody td {{
    padding: 0.875rem 1rem;
    text-align: center;
    border-bottom: 1px solid var(--border);
    font-size: 0.95rem;
  }}
  tbody td:first-child {{
    text-align: left;
    font-weight: 600;
    color: var(--primary);
  }}
  tbody tr:hover {{
    background: #edf2f7;
  }}
  tbody tr:nth-child(even) {{
    background: #f8fafc;
  }}
  tbody tr:nth-child(even):hover {{
    background: #edf2f7;
  }}
  tfoot td {{
    padding: 1rem;
    text-align: center;
    font-weight: 700;
    background: #edf2f7;
    border-top: 2px solid var(--primary);
    font-size: 1rem;
  }}
  tfoot td:first-child {{
    text-align: left;
    border-radius: 0 0 0 8px;
  }}
  tfoot td:last-child {{
    border-radius: 0 0 8px 0;
    color: var(--success);
  }}
  .zero {{ color: var(--text-light); }}
  .has-hours {{ color: var(--text); font-weight: 600; }}
  .section-title {{
    font-size: 1rem;
    font-weight: 700;
    color: var(--primary);
    margin-bottom: 0.75rem;
    padding-bottom: 0.5rem;
    border-bottom: 2px solid var(--accent);
    display: inline-block;
  }}
  .notes {{
    margin-top: 2rem;
    padding: 1.25rem;
    background: #fffff0;
    border: 1px solid #fefcbf;
    border-left: 4px solid var(--warning);
    border-radius: 0 8px 8px 0;
    font-size: 0.875rem;
  }}
  .notes h3 {{
    color: var(--warning);
    margin-bottom: 0.5rem;
    font-size: 0.9rem;
  }}
  .notes ul {{
    margin-left: 1.25rem;
    color: var(--text-light);
  }}
  .notes li {{
    margin-bottom: 0.25rem;
  }}
  .footer {{
    margin-top: 1.5rem;
    text-align: center;
    font-size: 0.8rem;
    color: var(--text-light);
  }}
  @media print {{
    body {{ padding: 0; background: white; }}
    .card {{ box-shadow: none; border: none; }}
    .header {{ border-radius: 0; }}
    tbody tr:hover {{ background: inherit; }}
  }}
</style>
</head>
<body>
<div class="container">
  <div class="header">
    <div>
      <h1>Monthly Man-Hours Report</h1>
      <p style="opacity:0.85; margin-top:0.25rem; font-size:0.9rem;">Resource Allocation &amp; P&amp;L Analysis</p>
    </div>
    <div class="meta">
      <div class="period">{month_label}</div>
      <div>Generated: {datetime.now().strftime("%b %d, %Y %H:%M")}</div>
    </div>
  </div>
  <div class="card">
    <!-- Summary Cards -->
    <div class="summary-grid">
"""

    # Summary boxes per resource type
    for rtype in RESOURCE_TYPES:
        val = totals["resource_totals"].get(rtype, 0.0)
        html += f"""      <div class="summary-box">
        <div class="label">{rtype} Hours</div>
        <div class="value">{val:,.1f}</div>
      </div>
"""

    html += f"""      <div class="summary-box grand">
        <div class="label">Grand Total</div>
        <div class="value">{totals["grand_total"]:,.1f}</div>
      </div>
    </div>

    <!-- Main Data Table -->
    <div class="section-title">Man-Hours by Project &amp; Resource Type</div>
    <table>
      <thead>
        <tr>
          <th>Project</th>
"""
    for rtype in RESOURCE_TYPES:
        html += f"          <th>{rtype}</th>\n"
    html += "          <th>Total</th>\n        </tr>\n      </thead>\n      <tbody>\n"

    for project in PROJECTS:
        html += f"        <tr>\n          <td>{project}</td>\n"
        for rtype in RESOURCE_TYPES:
            hours = data.get(project, {}).get(rtype, 0.0)
            css_class = "has-hours" if hours > 0 else "zero"
            html += f'          <td class="{css_class}">{hours:,.1f}</td>\n'
        pt = totals["project_totals"][project]
        css_class = "has-hours" if pt > 0 else "zero"
        html += f'          <td class="{css_class}">{pt:,.1f}</td>\n'
        html += "        </tr>\n"

    html += "      </tbody>\n      <tfoot>\n        <tr>\n          <td>TOTAL</td>\n"
    for rtype in RESOURCE_TYPES:
        html += f'          <td>{totals["resource_totals"].get(rtype, 0.0):,.1f}</td>\n'
    html += f'          <td>{totals["grand_total"]:,.1f}</td>\n'
    html += """        </tr>
      </tfoot>
    </table>

    <!-- Instructions / Notes -->
    <div class="notes">
      <h3>Action Items &amp; Notes</h3>
      <ul>
        <li><strong>Team Leads:</strong> Please fill in actual man-hours for each resource type per project.</li>
        <li><strong>Accuracy:</strong> Ensure hours reflect actual effort, even if resources moved between projects mid-month.</li>
        <li><strong>Validation:</strong> Each team lead must validate their section before final submission.</li>
        <li><strong>Deadline:</strong> January 2026 data must be finalized and submitted by the next Thursday.</li>
        <li><strong>Purpose:</strong> This data feeds into P&amp;L analysis — accuracy is critical.</li>
        <li><strong>Cadence:</strong> Monthly reporting starts January 2026 and continues each month.</li>
      </ul>
    </div>

    <div class="footer">
      <p>Prepared for P&amp;L Analysis &bull; Confidential</p>
    </div>
  </div>
</div>
</body>
</html>"""

    with open(output_path, "w") as f:
        f.write(html)

    print(f"  HTML report saved to: {output_path}")


def generate_markdown_report(data: dict, month_label: str, output_path: str):
    """Generate a Markdown version of the report (good for Teams/Slack sharing)."""
    totals = compute_totals(data)

    lines = []
    lines.append(f"# Monthly Man-Hours Report — {month_label}")
    lines.append("")
    lines.append(f"**Generated:** {datetime.now().strftime('%B %d, %Y at %H:%M')}")
    lines.append(f"**Purpose:** Resource allocation tracking for P&L analysis")
    lines.append("")

    # Summary
    lines.append("## Summary")
    lines.append("")
    for rtype in RESOURCE_TYPES:
        val = totals["resource_totals"].get(rtype, 0.0)
        lines.append(f"- **{rtype} Total:** {val:,.1f} hours")
    lines.append(f"- **Grand Total:** {totals['grand_total']:,.1f} hours")
    lines.append("")

    # Table
    lines.append("## Detailed Breakdown")
    lines.append("")
    header = "| Project | " + " | ".join(RESOURCE_TYPES) + " | Total |"
    separator = "|" + "|".join(["---"] * (len(RESOURCE_TYPES) + 2)) + "|"
    lines.append(header)
    lines.append(separator)

    for project in PROJECTS:
        cols = [project]
        for rtype in RESOURCE_TYPES:
            hours = data.get(project, {}).get(rtype, 0.0)
            cols.append(f"{hours:,.1f}")
        cols.append(f"{totals['project_totals'][project]:,.1f}")
        lines.append("| " + " | ".join(cols) + " |")

    # Totals
    total_cols = ["**TOTAL**"]
    for rtype in RESOURCE_TYPES:
        total_cols.append(f"**{totals['resource_totals'].get(rtype, 0.0):,.1f}**")
    total_cols.append(f"**{totals['grand_total']:,.1f}**")
    lines.append("| " + " | ".join(total_cols) + " |")
    lines.append("")

    # Notes
    lines.append("## Action Items")
    lines.append("")
    lines.append("- [ ] Team Leads: Fill in actual man-hours for each resource type per project")
    lines.append("- [ ] Ensure hours reflect actual effort (even if resources moved between projects)")
    lines.append("- [ ] Each team lead validates their section before final submission")
    lines.append("- [ ] January 2026 data finalized by next Thursday")
    lines.append("- [ ] Monthly cadence: repeat this process each month going forward")
    lines.append("")
    lines.append("---")
    lines.append("*Prepared for P&L Analysis — Confidential*")
    lines.append("")

    with open(output_path, "w") as f:
        f.write("\n".join(lines))

    print(f"  Markdown report saved to: {output_path}")


# ─────────────────────────────────────────────────────────────────────────────
# DATA INPUT TEMPLATE (for quick editing)
# ─────────────────────────────────────────────────────────────────────────────

def generate_input_template_csv(output_path: str):
    """Generate a blank CSV template that team leads can fill in."""
    with open(output_path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["Project", "Resource Type", "Man-Hours", "Team Lead Name", "Validated (Y/N)", "Notes"])
        for project in PROJECTS:
            for rtype in RESOURCE_TYPES:
                writer.writerow([project, rtype, "", "", "", ""])
    print(f"  Input template saved to: {output_path}")


# ─────────────────────────────────────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────────────────────────────────────

def main():
    """Generate all report formats for January 2026."""
    month_label = get_month_label(2026, 1)
    data = JANUARY_2026_DATA

    # Create output directory
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "reports")
    os.makedirs(output_dir, exist_ok=True)

    print(f"Generating Monthly Man-Hours Report — {month_label}")
    print(f"Projects: {', '.join(PROJECTS)}")
    print(f"Resource Types: {', '.join(RESOURCE_TYPES)}")
    print(f"Output directory: {output_dir}")
    print()

    # 1. Input template for team leads
    print("[1/5] Input template (for team leads to fill in):")
    generate_input_template_csv(os.path.join(output_dir, "input_template_man_hours.csv"))

    # 2. CSV report
    print("[2/5] CSV report:")
    generate_csv_report(data, month_label, os.path.join(output_dir, f"man_hours_report_{month_label.replace(' ', '_').lower()}.csv"))

    # 3. JSON report
    print("[3/5] JSON report:")
    generate_json_report(data, month_label, os.path.join(output_dir, f"man_hours_report_{month_label.replace(' ', '_').lower()}.json"))

    # 4. HTML report (professional, printable)
    print("[4/5] HTML report:")
    generate_html_report(data, month_label, os.path.join(output_dir, f"man_hours_report_{month_label.replace(' ', '_').lower()}.html"))

    # 5. Markdown report (Teams/Slack friendly)
    print("[5/5] Markdown report:")
    generate_markdown_report(data, month_label, os.path.join(output_dir, f"man_hours_report_{month_label.replace(' ', '_').lower()}.md"))

    print()
    print("All reports generated successfully!")
    print()
    print("NEXT STEPS:")
    print("  1. Share 'input_template_man_hours.csv' with your team leads")
    print("  2. Collect filled templates and update JANUARY_2026_DATA in this script")
    print("  3. Re-run the script to generate final reports")
    print("  4. Use the HTML report for management presentations")
    print("  5. Use the JSON report for system/dashboard integration")
    print("  6. Use the Markdown report for Teams/Slack sharing")


if __name__ == "__main__":
    main()
