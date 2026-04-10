#!/usr/bin/env python3
"""
Daily Team Tracker — Excel Generator
======================================
Creates a single .xlsx file that a Tech Manager can open daily to:
  - Log daily work per person (what they did, hours, project, status)
  - See the full team roster (who is on which project)
  - Track monthly KPIs per person
  - Record progress, feedback, errors, achievements
  - View auto-calculated summaries

Sheets:
  1. DAILY LOG          — Day-by-day work entries (the main daily-use sheet)
  2. TEAM ROSTER        — All people, teams, projects, roles at a glance
  3. MONTHLY KPI        — KPI numbers per person with auto-formulas
  4. PROGRESS & FEEDBACK — Feedback, achievements, errors per person
  5. PROJECT SUMMARY    — Hours and tasks rolled up per project
  6. INSTRUCTIONS       — How to use this file

Usage:
    python3 generate_excel_tracker.py

    Opens in Excel / Google Sheets / LibreOffice Calc.
    Update daily — no scripts needed after generation.
"""

import json
import os
from datetime import datetime, date, timedelta

import openpyxl
from openpyxl.styles import (
    Font, PatternFill, Alignment, Border, Side, numbers
)
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation
from openpyxl.formatting.rule import CellIsRule

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DATA_FILE = os.path.join(SCRIPT_DIR, "team_kpi_data.json")
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "reports")


def load_data():
    with open(DATA_FILE, "r") as f:
        return json.load(f)


# ─── Styles ──────────────────────────────────────────────────────────────────

BLUE_DARK = "1A365D"
BLUE_MED = "2B6CB0"
BLUE_LIGHT = "EBF4FF"
GREEN = "38A169"
GREEN_LIGHT = "F0FFF4"
RED = "E53E3E"
RED_LIGHT = "FFF5F5"
YELLOW = "D69E2E"
YELLOW_LIGHT = "FFFFF0"
PURPLE = "805AD5"
PURPLE_LIGHT = "FAF5FF"
ORANGE = "DD6B20"
GRAY = "718096"
GRAY_LIGHT = "F7FAFC"
WHITE = "FFFFFF"

HEADER_FONT = Font(name="Calibri", size=11, bold=True, color=WHITE)
HEADER_FILL = PatternFill("solid", fgColor=BLUE_DARK)
HEADER_ALIGN = Alignment(horizontal="center", vertical="center", wrap_text=True)

SUBHEADER_FONT = Font(name="Calibri", size=10, bold=True, color=BLUE_DARK)
SUBHEADER_FILL = PatternFill("solid", fgColor=BLUE_LIGHT)

DATA_FONT = Font(name="Calibri", size=10)
DATA_FONT_BOLD = Font(name="Calibri", size=10, bold=True)
DATA_ALIGN = Alignment(horizontal="center", vertical="center")
DATA_ALIGN_LEFT = Alignment(horizontal="left", vertical="center", wrap_text=True)

TITLE_FONT = Font(name="Calibri", size=14, bold=True, color=BLUE_DARK)
SUBTITLE_FONT = Font(name="Calibri", size=10, color=GRAY)

THIN_BORDER = Border(
    left=Side(style="thin", color="E2E8F0"),
    right=Side(style="thin", color="E2E8F0"),
    top=Side(style="thin", color="E2E8F0"),
    bottom=Side(style="thin", color="E2E8F0"),
)

TOTAL_FILL = PatternFill("solid", fgColor="EDF2F7")
TOTAL_FONT = Font(name="Calibri", size=10, bold=True, color=BLUE_DARK)

GOOD_FILL = PatternFill("solid", fgColor=GREEN_LIGHT)
BAD_FILL = PatternFill("solid", fgColor=RED_LIGHT)
WARN_FILL = PatternFill("solid", fgColor=YELLOW_LIGHT)


def style_header_row(ws, row, max_col):
    for col in range(1, max_col + 1):
        cell = ws.cell(row=row, column=col)
        cell.font = HEADER_FONT
        cell.fill = HEADER_FILL
        cell.alignment = HEADER_ALIGN
        cell.border = THIN_BORDER


def style_data_cell(ws, row, col, align="center", bold=False):
    cell = ws.cell(row=row, column=col)
    cell.font = DATA_FONT_BOLD if bold else DATA_FONT
    cell.alignment = DATA_ALIGN if align == "center" else DATA_ALIGN_LEFT
    cell.border = THIN_BORDER
    return cell


def add_title(ws, title, subtitle="", row=1):
    ws.cell(row=row, column=1, value=title).font = TITLE_FONT
    if subtitle:
        ws.cell(row=row + 1, column=1, value=subtitle).font = SUBTITLE_FONT
    return row + (3 if subtitle else 2)


def auto_width(ws, min_width=10, max_width=35):
    for col in ws.columns:
        col_letter = get_column_letter(col[0].column)
        max_len = min_width
        for cell in col:
            if cell.value:
                max_len = max(max_len, min(len(str(cell.value)) + 2, max_width))
        ws.column_dimensions[col_letter].width = max_len


# ─── Sheet Builders ──────────────────────────────────────────────────────────

def build_daily_log(wb, data):
    """Sheet 1: DAILY LOG — the main sheet you update every day."""
    ws = wb.active
    ws.title = "Daily Log"
    ws.sheet_properties.tabColor = BLUE_MED

    teams = data["teams"]
    projects = data["projects"]
    all_members = []
    for team_name, team in teams.items():
        for member_name, member in team["members"].items():
            all_members.append((team_name, team["project"], team["tech_lead"], member_name, member))

    unique_names = sorted(set(m[3] for m in all_members))

    r = add_title(ws, "Daily Work Log", f"Tech Manager: {data['tech_manager']} | Update this sheet every day")

    headers = [
        "Date", "Day", "Name", "Team", "Project",
        "Task / Ticket #", "Task Description",
        "Status", "Hours Worked", "Extra Hours",
        "Blockers / Issues", "Notes / Progress",
    ]
    for ci, h in enumerate(headers, 1):
        ws.cell(row=r, column=ci, value=h)
    style_header_row(ws, r, len(headers))
    ws.row_dimensions[r].height = 30

    # Freeze panes
    ws.freeze_panes = f"A{r + 1}"

    # Data validations
    name_list = ",".join(unique_names)
    name_dv = DataValidation(type="list", formula1=f'"{name_list}"', allow_blank=True)
    name_dv.error = "Pick a team member from the list"
    name_dv.errorTitle = "Invalid Name"
    name_dv.prompt = "Select team member"
    name_dv.promptTitle = "Name"
    ws.add_data_validation(name_dv)

    proj_list = ",".join(projects)
    proj_dv = DataValidation(type="list", formula1=f'"{proj_list}"', allow_blank=True)
    proj_dv.prompt = "Select project"
    ws.add_data_validation(proj_dv)

    status_dv = DataValidation(
        type="list",
        formula1='"Not Started,In Progress,In Review,Blocked,Completed,Carry Forward"',
        allow_blank=True,
    )
    status_dv.prompt = "Select status"
    ws.add_data_validation(status_dv)

    team_list = ",".join(teams.keys())
    team_dv = DataValidation(type="list", formula1=f'"{team_list}"', allow_blank=True)
    ws.add_data_validation(team_dv)

    # Pre-fill ~500 rows with formatting and date formulas for Jan 2026 onwards
    year = data["report_year"]
    start_date = date(year, 1, 1)
    day_names = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]

    prefill_rows = 500
    data_start = r + 1

    for i in range(prefill_rows):
        row = data_start + i
        for ci in range(1, len(headers) + 1):
            style_data_cell(ws, row, ci, align="center" if ci <= 5 or ci in [8, 9, 10] else "left")

        # Alternate row shading
        if i % 2 == 0:
            for ci in range(1, len(headers) + 1):
                ws.cell(row=row, column=ci).fill = PatternFill("solid", fgColor=GRAY_LIGHT)

        name_dv.add(ws.cell(row=row, column=3))
        team_dv.add(ws.cell(row=row, column=4))
        proj_dv.add(ws.cell(row=row, column=5))
        status_dv.add(ws.cell(row=row, column=8))

        ws.cell(row=row, column=9).number_format = "0.0"
        ws.cell(row=row, column=10).number_format = "0.0"

    # Pre-fill first 31 days (Jan 2026) — one row per person per day for first 5 days as example
    example_row = data_start
    for day_offset in range(31):
        d = start_date + timedelta(days=day_offset)
        if d.weekday() >= 5:
            continue
        ws.cell(row=example_row, column=1, value=d).number_format = "DD-MMM-YYYY"
        ws.cell(row=example_row, column=2, value=day_names[d.weekday()])
        example_row += 1

    # Conditional formatting for Status column (col 8)
    status_col = get_column_letter(8)
    range_str = f"{status_col}{data_start}:{status_col}{data_start + prefill_rows}"

    ws.conditional_formatting.add(range_str, CellIsRule(
        operator="equal", formula=['"Completed"'],
        fill=PatternFill("solid", fgColor=GREEN_LIGHT),
    ))
    ws.conditional_formatting.add(range_str, CellIsRule(
        operator="equal", formula=['"Blocked"'],
        fill=PatternFill("solid", fgColor=RED_LIGHT),
    ))
    ws.conditional_formatting.add(range_str, CellIsRule(
        operator="equal", formula=['"In Progress"'],
        fill=PatternFill("solid", fgColor=BLUE_LIGHT),
    ))
    ws.conditional_formatting.add(range_str, CellIsRule(
        operator="equal", formula=['"Carry Forward"'],
        fill=PatternFill("solid", fgColor=YELLOW_LIGHT),
    ))

    # Column widths
    widths = [14, 6, 16, 10, 14, 16, 40, 16, 12, 12, 30, 35]
    for ci, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(ci)].width = w


def build_team_roster(wb, data):
    """Sheet 2: TEAM ROSTER — who is on which project, their role, team."""
    ws = wb.create_sheet("Team Roster")
    ws.sheet_properties.tabColor = GREEN

    teams = data["teams"]
    r = add_title(ws, "Team Roster & Project Assignments",
                  f"{len(teams)} Teams | {data['report_year']}")

    headers = [
        "Team", "Project", "Tech Lead", "Member Name", "Role",
        "All Projects Assigned", "Multi-Project?",
        "Contact / Notes",
    ]
    for ci, h in enumerate(headers, 1):
        ws.cell(row=r, column=ci, value=h)
    style_header_row(ws, r, len(headers))
    ws.freeze_panes = f"A{r + 1}"

    row = r + 1
    prev_team = ""
    for team_name, team in teams.items():
        for member_name, member in team["members"].items():
            projs = ", ".join(member.get("projects_assigned", []))
            is_multi = "Yes" if len(member.get("projects_assigned", [])) > 1 else ""

            for ci in range(1, len(headers) + 1):
                style_data_cell(ws, row, ci,
                                align="left" if ci in [4, 6, 8] else "center",
                                bold=("Lead" in member.get("role", "")))

            ws.cell(row=row, column=1, value=team_name)
            ws.cell(row=row, column=2, value=team["project"])
            ws.cell(row=row, column=3, value=team["tech_lead"])
            ws.cell(row=row, column=4, value=member_name)
            ws.cell(row=row, column=5, value=member.get("role", ""))
            ws.cell(row=row, column=6, value=projs)
            ws.cell(row=row, column=7, value=is_multi)
            ws.cell(row=row, column=8, value="")

            if is_multi:
                ws.cell(row=row, column=7).fill = PatternFill("solid", fgColor=PURPLE_LIGHT)
                ws.cell(row=row, column=7).font = Font(name="Calibri", size=10, bold=True, color=PURPLE)

            # Team group shading
            if team_name != prev_team and prev_team:
                for ci in range(1, len(headers) + 1):
                    ws.cell(row=row, column=ci).fill = PatternFill("solid", fgColor=BLUE_LIGHT)
            prev_team = team_name

            row += 1

    # Summary below
    row += 1
    ws.cell(row=row, column=1, value="SUMMARY").font = SUBHEADER_FONT
    row += 1

    unique_people = set()
    multi_people = set()
    for team in teams.values():
        for mname, m in team["members"].items():
            unique_people.add(mname)
            if len(m.get("projects_assigned", [])) > 1:
                multi_people.add(mname)

    ws.cell(row=row, column=1, value="Total Teams:").font = DATA_FONT_BOLD
    ws.cell(row=row, column=2, value=len(teams)).font = DATA_FONT_BOLD
    row += 1
    ws.cell(row=row, column=1, value="Unique People:").font = DATA_FONT_BOLD
    ws.cell(row=row, column=2, value=len(unique_people)).font = DATA_FONT_BOLD
    row += 1
    ws.cell(row=row, column=1, value="Multi-Project:").font = DATA_FONT_BOLD
    ws.cell(row=row, column=2, value=len(multi_people)).font = DATA_FONT_BOLD
    row += 1
    if multi_people:
        ws.cell(row=row, column=1, value="Who:").font = DATA_FONT
        ws.cell(row=row, column=2, value=", ".join(sorted(multi_people))).font = DATA_FONT

    widths = [12, 14, 16, 18, 12, 30, 14, 25]
    for ci, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(ci)].width = w


def build_monthly_kpi(wb, data):
    """Sheet 3: MONTHLY KPI — scorecards with formulas."""
    ws = wb.create_sheet("Monthly KPI")
    ws.sheet_properties.tabColor = YELLOW

    teams = data["teams"]
    month = data["report_month"]
    year = data["report_year"]

    r = add_title(ws, f"Monthly KPI Scorecard — {month} {year}",
                  "Fill in KPIs for each person. Completion % and Bug Ratio auto-calculate.")

    headers = [
        "Team", "Project", "Name", "Role",
        "Tasks Assigned", "Tasks Completed", "Completion %",
        "Bugs in Work", "Bugs Fixed", "Bug Ratio",
        "Code Reviews", "On-Time %",
        "Quality (1-5)", "Total Hours", "Extra Hours",
        "Overall Grade",
    ]
    for ci, h in enumerate(headers, 1):
        ws.cell(row=r, column=ci, value=h)
    style_header_row(ws, r, len(headers))
    ws.row_dimensions[r].height = 35
    ws.freeze_panes = f"A{r + 1}"

    row = r + 1
    data_start_row = row

    for team_name, team in teams.items():
        for member_name, member in team["members"].items():
            kpi = member.get("kpi", {})

            for ci in range(1, len(headers) + 1):
                c = style_data_cell(ws, row, ci,
                                    align="left" if ci == 3 else "center",
                                    bold=("Lead" in member.get("role", "")))

            ws.cell(row=row, column=1, value=team_name)
            ws.cell(row=row, column=2, value=team["project"])
            ws.cell(row=row, column=3, value=member_name)
            ws.cell(row=row, column=4, value=member.get("role", ""))

            # Editable KPI cells (user fills these in)
            ws.cell(row=row, column=5, value=kpi.get("tasks_assigned", 0))
            ws.cell(row=row, column=6, value=kpi.get("tasks_completed", 0))

            # Completion % = auto formula
            e_col = get_column_letter(5)  # tasks_assigned
            f_col = get_column_letter(6)  # tasks_completed
            ws.cell(row=row, column=7).value = f'=IF({e_col}{row}=0,"",{f_col}{row}/{e_col}{row})'
            ws.cell(row=row, column=7).number_format = "0%"

            ws.cell(row=row, column=8, value=kpi.get("bugs_found_in_work", 0))
            ws.cell(row=row, column=9, value=kpi.get("bugs_fixed", 0))

            # Bug Ratio = auto formula
            h_col = get_column_letter(8)  # bugs_in
            ws.cell(row=row, column=10).value = f'=IF({f_col}{row}=0,"",{h_col}{row}/{f_col}{row})'
            ws.cell(row=row, column=10).number_format = "0.00"

            ws.cell(row=row, column=11, value=kpi.get("code_reviews_done", 0))
            ws.cell(row=row, column=12, value=kpi.get("on_time_delivery_pct", 0))
            ws.cell(row=row, column=12).number_format = "0\"%\""

            ws.cell(row=row, column=13, value=kpi.get("quality_score", 0))
            ws.cell(row=row, column=14, value=kpi.get("total_hours_worked", 0))
            ws.cell(row=row, column=14).number_format = "0.0"
            ws.cell(row=row, column=15, value=kpi.get("extra_hours", 0))
            ws.cell(row=row, column=15).number_format = "0.0"

            # Overall Grade formula (weighted: completion 30%, on-time 30%, quality 25%, bug 15%)
            g_col = get_column_letter(7)   # completion %
            l_col = get_column_letter(12)  # on-time %
            m_col = get_column_letter(13)  # quality
            j_col = get_column_letter(10)  # bug ratio

            grade_formula = (
                f'=IF({e_col}{row}=0,"-",'
                f'IF(({g_col}{row}*30)+({l_col}{row}/100*30)+({m_col}{row}/5*25)+'
                f'((1-MIN(IF({j_col}{row}="",0,{j_col}{row}),1))*15)>=85,"A",'
                f'IF(({g_col}{row}*30)+({l_col}{row}/100*30)+({m_col}{row}/5*25)+'
                f'((1-MIN(IF({j_col}{row}="",0,{j_col}{row}),1))*15)>=70,"B",'
                f'IF(({g_col}{row}*30)+({l_col}{row}/100*30)+({m_col}{row}/5*25)+'
                f'((1-MIN(IF({j_col}{row}="",0,{j_col}{row}),1))*15)>=55,"C",'
                f'IF(({g_col}{row}*30)+({l_col}{row}/100*30)+({m_col}{row}/5*25)+'
                f'((1-MIN(IF({j_col}{row}="",0,{j_col}{row}),1))*15)>=40,"D","F")))))'
            )
            ws.cell(row=row, column=16, value=grade_formula)
            ws.cell(row=row, column=16).font = Font(name="Calibri", size=12, bold=True)
            ws.cell(row=row, column=16).alignment = DATA_ALIGN

            row += 1

    data_end_row = row - 1

    # Conditional formatting for Completion % (col 7)
    comp_col = get_column_letter(7)
    comp_range = f"{comp_col}{data_start_row}:{comp_col}{data_end_row}"
    ws.conditional_formatting.add(comp_range, CellIsRule(
        operator="greaterThanOrEqual", formula=["0.8"], fill=GOOD_FILL))
    ws.conditional_formatting.add(comp_range, CellIsRule(
        operator="lessThan", formula=["0.5"], fill=BAD_FILL))

    # Conditional formatting for Grade (col 16)
    grade_col = get_column_letter(16)
    grade_range = f"{grade_col}{data_start_row}:{grade_col}{data_end_row}"
    ws.conditional_formatting.add(grade_range, CellIsRule(
        operator="equal", formula=['"A"'], fill=GOOD_FILL))
    ws.conditional_formatting.add(grade_range, CellIsRule(
        operator="equal", formula=['"B"'], fill=GOOD_FILL))
    ws.conditional_formatting.add(grade_range, CellIsRule(
        operator="equal", formula=['"C"'], fill=WARN_FILL))
    ws.conditional_formatting.add(grade_range, CellIsRule(
        operator="equal", formula=['"D"'], fill=WARN_FILL))
    ws.conditional_formatting.add(grade_range, CellIsRule(
        operator="equal", formula=['"F"'], fill=BAD_FILL))

    # Quality score validation (1-5)
    q_dv = DataValidation(type="whole", operator="between", formula1="0", formula2="5", allow_blank=True)
    q_dv.error = "Quality score must be between 1 and 5"
    ws.add_data_validation(q_dv)
    for r_idx in range(data_start_row, data_end_row + 1):
        q_dv.add(ws.cell(row=r_idx, column=13))

    # TOTALS row
    row += 1
    for ci in range(1, len(headers) + 1):
        ws.cell(row=row, column=ci).fill = TOTAL_FILL
        ws.cell(row=row, column=ci).font = TOTAL_FONT
        ws.cell(row=row, column=ci).border = THIN_BORDER
        ws.cell(row=row, column=ci).alignment = DATA_ALIGN

    ws.cell(row=row, column=1, value="TOTALS")
    for sum_col in [5, 6, 8, 9, 11, 14, 15]:
        col_l = get_column_letter(sum_col)
        ws.cell(row=row, column=sum_col, value=f"=SUM({col_l}{data_start_row}:{col_l}{data_end_row})")

    widths = [12, 14, 18, 12, 13, 14, 13, 12, 11, 11, 12, 11, 12, 12, 12, 13]
    for ci, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(ci)].width = w


def build_progress_feedback(wb, data):
    """Sheet 4: PROGRESS & FEEDBACK — per person text fields."""
    ws = wb.create_sheet("Progress & Feedback")
    ws.sheet_properties.tabColor = PURPLE

    teams = data["teams"]
    month = data["report_month"]
    year = data["report_year"]

    r = add_title(ws, f"Progress, Feedback & Achievements — {month} {year}",
                  "Record achievements, errors, feedback from Tech Lead and Manager")

    headers = [
        "Team", "Project", "Name", "Role",
        "Achievements This Month",
        "Errors / Issues Log",
        "Tech Lead Feedback",
        "Manager Feedback",
        "Progress Notes",
        "Action Items / Next Steps",
    ]
    for ci, h in enumerate(headers, 1):
        ws.cell(row=r, column=ci, value=h)
    style_header_row(ws, r, len(headers))
    ws.row_dimensions[r].height = 30
    ws.freeze_panes = f"A{r + 1}"

    row = r + 1
    for team_name, team in teams.items():
        for member_name, member in team["members"].items():
            for ci in range(1, len(headers) + 1):
                style_data_cell(ws, row, ci,
                                align="left" if ci >= 5 else "center",
                                bold=("Lead" in member.get("role", "")))

            ws.cell(row=row, column=1, value=team_name)
            ws.cell(row=row, column=2, value=team["project"])
            ws.cell(row=row, column=3, value=member_name)
            ws.cell(row=row, column=4, value=member.get("role", ""))
            ws.cell(row=row, column=5, value=member.get("achievements", ""))
            ws.cell(row=row, column=6, value=member.get("errors_log", ""))
            ws.cell(row=row, column=7, value=member.get("tech_lead_feedback", ""))
            ws.cell(row=row, column=8, value=member.get("manager_feedback", ""))
            ws.cell(row=row, column=9, value=member.get("progress_notes", ""))
            ws.cell(row=row, column=10, value="")

            ws.row_dimensions[row].height = 50
            row += 1

    widths = [12, 14, 18, 12, 35, 35, 35, 35, 35, 30]
    for ci, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(ci)].width = w


def build_project_summary(wb, data):
    """Sheet 5: PROJECT SUMMARY — auto-calculated from KPI sheet."""
    ws = wb.create_sheet("Project Summary")
    ws.sheet_properties.tabColor = ORANGE

    teams = data["teams"]
    projects = data["projects"]
    year = data["report_year"]
    month = data["report_month"]

    r = add_title(ws, f"Project Summary — {month} {year}",
                  "Hours and headcount per project (auto-calculated from roster)")

    headers = [
        "Project", "Teams", "Total Members", "Tech Lead(s)",
        "Total Hours", "Extra Hours", "Tasks Assigned", "Tasks Completed",
    ]
    for ci, h in enumerate(headers, 1):
        ws.cell(row=r, column=ci, value=h)
    style_header_row(ws, r, len(headers))

    row = r + 1
    grand = {"members": 0, "hours": 0, "extra": 0, "assigned": 0, "completed": 0}

    for project in projects:
        proj_teams = []
        proj_leads = set()
        proj_members = 0
        proj_hours = 0
        proj_extra = 0
        proj_assigned = 0
        proj_completed = 0

        for team_name, team in teams.items():
            if team["project"] == project:
                proj_teams.append(team_name)
                proj_leads.add(team["tech_lead"])
                for m in team["members"].values():
                    proj_members += 1
                    kpi = m.get("kpi", {})
                    proj_hours += kpi.get("total_hours_worked", 0)
                    proj_extra += kpi.get("extra_hours", 0)
                    proj_assigned += kpi.get("tasks_assigned", 0)
                    proj_completed += kpi.get("tasks_completed", 0)

        for ci in range(1, len(headers) + 1):
            style_data_cell(ws, row, ci, align="left" if ci in [1, 2, 4] else "center", bold=True)

        ws.cell(row=row, column=1, value=project)
        ws.cell(row=row, column=2, value=", ".join(proj_teams))
        ws.cell(row=row, column=3, value=proj_members)
        ws.cell(row=row, column=4, value=", ".join(sorted(proj_leads)))
        ws.cell(row=row, column=5, value=proj_hours)
        ws.cell(row=row, column=6, value=proj_extra)
        ws.cell(row=row, column=7, value=proj_assigned)
        ws.cell(row=row, column=8, value=proj_completed)

        grand["members"] += proj_members
        grand["hours"] += proj_hours
        grand["extra"] += proj_extra
        grand["assigned"] += proj_assigned
        grand["completed"] += proj_completed

        row += 1

    # Grand total row
    row += 1
    for ci in range(1, len(headers) + 1):
        ws.cell(row=row, column=ci).fill = TOTAL_FILL
        ws.cell(row=row, column=ci).font = TOTAL_FONT
        ws.cell(row=row, column=ci).border = THIN_BORDER
        ws.cell(row=row, column=ci).alignment = DATA_ALIGN

    ws.cell(row=row, column=1, value="GRAND TOTAL")
    ws.cell(row=row, column=3, value=grand["members"])
    ws.cell(row=row, column=5, value=grand["hours"])
    ws.cell(row=row, column=6, value=grand["extra"])
    ws.cell(row=row, column=7, value=grand["assigned"])
    ws.cell(row=row, column=8, value=grand["completed"])

    widths = [16, 22, 14, 22, 14, 14, 16, 18]
    for ci, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(ci)].width = w


def build_instructions(wb, data):
    """Sheet 6: INSTRUCTIONS — how to use this file."""
    ws = wb.create_sheet("Instructions")
    ws.sheet_properties.tabColor = GRAY

    lines = [
        ("HOW TO USE THIS FILE", TITLE_FONT),
        ("", None),
        ("This is your single Excel file for daily team tracking.", SUBTITLE_FONT),
        ("", None),
        ("DAILY (Every Day):", SUBHEADER_FONT),
        ("  1. Go to the 'Daily Log' sheet", DATA_FONT),
        ("  2. Add a row for each person's work today", DATA_FONT),
        ("  3. Pick their name from the dropdown (column C)", DATA_FONT),
        ("  4. Select the project from the dropdown (column E)", DATA_FONT),
        ("  5. Enter task description, hours, and status", DATA_FONT),
        ("  6. Status dropdown: Not Started, In Progress, In Review, Blocked, Completed, Carry Forward", DATA_FONT),
        ("  7. Colors auto-apply: green=Completed, red=Blocked, blue=In Progress, yellow=Carry Forward", DATA_FONT),
        ("", None),
        ("WEEKLY / AS NEEDED:", SUBHEADER_FONT),
        ("  - Update 'Monthly KPI' sheet with latest numbers", DATA_FONT),
        ("  - Completion % and Bug Ratio calculate automatically", DATA_FONT),
        ("  - Overall Grade (A-F) calculates automatically", DATA_FONT),
        ("  - Check 'Progress & Feedback' sheet — add feedback and achievements", DATA_FONT),
        ("", None),
        ("MONTHLY:", SUBHEADER_FONT),
        ("  - Review all KPIs in 'Monthly KPI' sheet", DATA_FONT),
        ("  - Fill in Tech Lead and Manager feedback for each person", DATA_FONT),
        ("  - Review 'Project Summary' for project-level totals", DATA_FONT),
        ("  - Save a copy as archive before starting the next month", DATA_FONT),
        ("", None),
        ("SHEET DESCRIPTIONS:", SUBHEADER_FONT),
        ("  Daily Log         — Main daily sheet. Log what each person did, hours, status", DATA_FONT),
        ("  Team Roster       — All 21 people, their teams, projects, roles", DATA_FONT),
        ("  Monthly KPI       — KPI scorecard with auto-formulas for grades", DATA_FONT),
        ("  Progress & Feedback — Achievements, errors, Tech Lead/Manager feedback", DATA_FONT),
        ("  Project Summary   — Hours and tasks rolled up per project", DATA_FONT),
        ("", None),
        ("ADDING NEW PEOPLE:", SUBHEADER_FONT),
        ("  1. Add them to 'Team Roster' sheet", DATA_FONT),
        ("  2. Add a row for them in 'Monthly KPI' sheet", DATA_FONT),
        ("  3. Add a row for them in 'Progress & Feedback' sheet", DATA_FONT),
        ("  4. They will appear in Daily Log dropdowns after re-generating", DATA_FONT),
        ("     (or manually update the name dropdown list)", DATA_FONT),
        ("", None),
        ("KPI GRADING SYSTEM:", SUBHEADER_FONT),
        ("  Completion Rate (30%) + On-Time Delivery (30%) + Quality Score (25%) + Bug Ratio (15%)", DATA_FONT),
        ("  A = 85%+  |  B = 70-84%  |  C = 55-69%  |  D = 40-54%  |  F = below 40%", DATA_FONT),
        ("", None),
        ("TIPS:", SUBHEADER_FONT),
        ("  - Use filters on the Daily Log to see one person or one project", DATA_FONT),
        ("  - Sort by Date to see chronological work history", DATA_FONT),
        ("  - Sort by Name to see all work by one person", DATA_FONT),
        ("  - Print any sheet directly — formatting is print-ready", DATA_FONT),
        ("  - Save as .xlsx to keep formulas and dropdowns", DATA_FONT),
        ("", None),
        (f"Generated: {datetime.now().strftime('%B %d, %Y %H:%M')}", SUBTITLE_FONT),
        (f"Tech Manager: {data['tech_manager']}", SUBTITLE_FONT),
    ]

    for i, (text, font) in enumerate(lines, 1):
        cell = ws.cell(row=i, column=1, value=text)
        if font:
            cell.font = font

    ws.column_dimensions["A"].width = 90


# ─── Main ────────────────────────────────────────────────────────────────────

def main():
    print("=" * 60)
    print("  DAILY TEAM TRACKER — EXCEL GENERATOR")
    print("=" * 60)
    print()

    data = load_data()
    year = data["report_year"]
    month = data["report_month"]

    print(f"  Manager : {data['tech_manager']}")
    print(f"  Period  : {month} {year}")
    print(f"  Teams   : {len(data['teams'])}")

    unique = set()
    for team in data["teams"].values():
        for m in team["members"]:
            unique.add(m)
    print(f"  People  : {len(unique)} unique")
    print()

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    wb = openpyxl.Workbook()

    print("[1/5] Building Daily Log sheet...")
    build_daily_log(wb, data)

    print("[2/5] Building Team Roster sheet...")
    build_team_roster(wb, data)

    print("[3/5] Building Monthly KPI sheet...")
    build_monthly_kpi(wb, data)

    print("[4/5] Building Progress & Feedback sheet...")
    build_progress_feedback(wb, data)

    print("[5/5] Building Project Summary sheet...")
    build_project_summary(wb, data)

    print("[+]   Building Instructions sheet...")
    build_instructions(wb, data)

    filename = f"team_daily_tracker_{month.lower()}_{year}.xlsx"
    filepath = os.path.join(OUTPUT_DIR, filename)
    wb.save(filepath)

    print()
    print(f"  Excel file saved to: {filepath}")
    print()
    print("  WHAT TO DO NOW:")
    print(f"  1. Open  reports/{filename}  in Excel or Google Sheets")
    print("  2. Go to 'Daily Log' sheet — start logging daily work")
    print("  3. Use dropdowns for Name, Project, Team, Status")
    print("  4. Update 'Monthly KPI' sheet weekly with latest numbers")
    print("  5. Fill in 'Progress & Feedback' as needed")
    print("  6. 'Project Summary' gives you per-project totals at a glance")
    print()


if __name__ == "__main__":
    main()
