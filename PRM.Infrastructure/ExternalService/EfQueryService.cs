using Microsoft.EntityFrameworkCore;
using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Employee;
using PRM.Application.DTOs.Project;
using PRM.Application.Interfaces.Service;
using PRM.Domain.Enums;
using PRM.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.ExternalService
{
    /// <summary>
    /// Implements IQueryService using EF Core Include/Join queries.
    /// Lives in Infrastructure — this is the only layer that knows about EF Core.
    /// </summary>
    public class EfQueryService(PRMTDbContext context) : IQueryService
    {
        // ── Phase 2 ───────────────────────────────────────────────

        public async Task<EmployeeDetailResponse?> GetEmployeeWithSkillsAsync(int employeeId, CancellationToken ct = default)
        {
            var employee = await context.Employees
                .Include(e => e.Skills)
                .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

            if (employee is null) return null;

            return new EmployeeDetailResponse(
                employee.Id, employee.FullName, employee.Email,
                employee.Department, employee.Designation,
                employee.Status.ToString(), employee.IsActive,
                employee.Skills.Select(s => new SkillResponse(
                    s.Id, s.SkillName, s.Category.ToString(), s.Proficiency.ToString()
                ))
            );
        }

        public async Task<ProjectDetailResponse?> GetProjectWithMilestonesAsync(int projectId, CancellationToken ct = default)
        {
            var project = await context.Projects
                .Include(p => p.Manager)
                .Include(p => p.Milestones.OrderBy(m => m.DueDate))
                .FirstOrDefaultAsync(p => p.Id == projectId, ct);

            if (project is null) return null;

            return new ProjectDetailResponse(
                project.Id, project.Name, project.Description,
                project.StartDate, project.EndDate,
                project.Status.ToString(), project.Health.ToString(),
                project.ManagerId, project.Manager.FullName,
                project.Milestones.Select(m => new MilestoneResponse(
                    m.Id, m.Title, m.DueDate, m.Status.ToString()
                ))
            );
        }

        public async Task<IEnumerable<ProjectSummaryResponse>> GetAllProjectsWithManagerAsync(CancellationToken ct = default)
        {
            return await context.Projects
                .Include(p => p.Manager)
                .OrderBy(p => p.Name)
                .Select(p => new ProjectSummaryResponse(
                    p.Id, p.Name, p.Description, p.StartDate, p.EndDate,
                    p.Status.ToString(), p.Health.ToString(),
                    p.ManagerId, p.Manager.FullName
                ))
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<AllocationResponse>> GetAllocationsAsync(
            int? employeeId, int? projectId, bool activeOnly, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = context.Allocations
                .Include(a => a.Employee)
                .Include(a => a.Project)
                .AsQueryable();

            if (activeOnly)
                query = query.Where(a => a.ToDate >= today);

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            if (projectId.HasValue)
                query = query.Where(a => a.ProjectId == projectId.Value);

            return await query
                .OrderBy(a => a.Employee.FullName)
                .ThenBy(a => a.Project.Name)
                .Select(a => new AllocationResponse(
                    a.Id, a.EmployeeId, a.Employee.FullName,
                    a.ProjectId, a.Project.Name,
                    a.UtilisationPercentage, a.FromDate, a.ToDate
                ))
                .ToListAsync(ct);
        }

        // ── Phase 3 — Resource Dashboard ─────────────────────────

        //public async Task<IEnumerable<EmployeeUtilisationRow>> GetActiveEmployeeUtilisationsAsync(CancellationToken ct = default)
        //{
        //    var today = DateOnly.FromDateTime(DateTime.UtcNow);

        //    // For each active employee, sum utilisation of overlapping allocations
        //    var rows = await context.Employees
        //        .Where(e => e.IsActive)
        //        .Select(e => new EmployeeUtilisationRow(
        //            e.Id,
        //            e.FullName,
        //            e.Department,
        //            e.Designation,
        //            e.Allocations
        //                .Where(a => a.FromDate <= today && a.ToDate >= today)
        //                .Sum(a => a.UtilisationPercentage)
        //        ))
        //        .ToListAsync(ct);

        //    return rows;
        //}

        //public async Task<IEnumerable<string>> GetRecentActivityTagsAsync(int employeeId, int weekCount, CancellationToken ct = default)
        //{
        //    var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7 * weekCount));

        //    var tagStrings = await context.TimesheetEntries
        //        .Where(te => te.Timesheet.EmployeeId == employeeId
        //                  && te.Timesheet.WeekStartDate >= cutoff
        //                  && !string.IsNullOrEmpty(te.ActivityTags))
        //        .Select(te => te.ActivityTags)
        //        .ToListAsync(ct);

        //    // Tags are stored comma-separated — split, trim, deduplicate
        //    return tagStrings
        //        .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
        //        .Select(t => t.Trim())
        //        .Where(t => !string.IsNullOrEmpty(t))
        //        .Distinct()
        //        .OrderBy(t => t)
        //        .ToList();
        //}

        //public async Task<IEnumerable<ActiveAllocationItem>> GetActiveAllocationsForEmployeeAsync(int employeeId, CancellationToken ct = default)
        //{
        //    var today = DateOnly.FromDateTime(DateTime.UtcNow);

        //    return await context.Allocations
        //        .Include(a => a.Project)
        //        .Where(a => a.EmployeeId == employeeId && a.ToDate >= today)
        //        .OrderBy(a => a.Project.Name)
        //        .Select(a => new ActiveAllocationItem(
        //            a.Id, a.Project.Name, a.UtilisationPercentage, a.FromDate, a.ToDate
        //        ))
        //        .ToListAsync(ct);
        //}

        //// ── Phase 3 — Manager Projects ────────────────────────────

        //public async Task<IEnumerable<ManagerProjectSummaryResponse>> GetManagerProjectsAsync(int managerEmployeeId, CancellationToken ct = default)
        //{
        //    return await context.Projects
        //        .Where(p => p.ManagerId == managerEmployeeId)
        //        .OrderBy(p => p.EndDate)
        //        .Select(p => new ManagerProjectSummaryResponse(p.Id, p.Name, p.EndDate, p.Health.ToString()))
        //        .ToListAsync(ct);
        //}

        //public async Task<ManagerProjectDetailResponse?> GetManagerProjectDetailAsync(int projectId, int managerEmployeeId, CancellationToken ct = default)
        //{
        //    var project = await context.Projects
        //        .Include(p => p.Milestones.OrderBy(m => m.DueDate))
        //        .Include(p => p.Allocations.Where(a => a.ToDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
        //            .ThenInclude(a => a.Employee)
        //        .FirstOrDefaultAsync(p => p.Id == projectId && p.ManagerId == managerEmployeeId, ct);

        //    if (project is null) return null;

        //    var today = DateOnly.FromDateTime(DateTime.UtcNow);

        //    // Build risk flags
        //    var flags = BuildRiskFlags(project.Milestones, today);

        //    var milestones = project.Milestones.Select(m => new MilestoneResponse(
        //        m.Id, m.Title, m.DueDate, m.Status.ToString()
        //    ));

        //    var resources = project.Allocations.Select(a => new AllocationOnProjectResponse(
        //        a.Id, a.EmployeeId, a.Employee.FullName, a.UtilisationPercentage, a.FromDate, a.ToDate
        //    ));

        //    return new ManagerProjectDetailResponse(
        //        project.Id, project.Name, project.StartDate, project.EndDate,
        //        project.Status.ToString(), project.Health.ToString(),
        //        flags, milestones, resources
        //    );
        //}

        ///// <summary>
        ///// Derives risk flags from milestone data.
        ///// The background scheduler sets Health on the Project entity;
        ///// these flags explain why on the detail screen.
        ///// </summary>
        //private static IEnumerable<RiskFlagItem> BuildRiskFlags(
        //    IEnumerable<Domain.Entities.Milestone> milestones,
        //    DateOnly today)
        //{
        //    var flags = new List<RiskFlagItem>();
        //    var milestoneList = milestones.ToList();

        //    var overdue = milestoneList
        //        .Where(m => m.Status != MilestoneStatus.Done && m.DueDate < today)
        //        .ToList();

        //    foreach (var m in overdue)
        //    {
        //        var days = today.DayNumber - m.DueDate.DayNumber;
        //        flags.Add(new RiskFlagItem(true, $"{m.Title} milestone is {days} day(s) overdue"));
        //    }

        //    var hasNotStarted = milestoneList.Any(m => m.Status == MilestoneStatus.NotStarted);
        //    var hasInProgress = milestoneList.Any(m => m.Status == MilestoneStatus.InProgress);

        //    if (!flags.Any())
        //        flags.Add(new RiskFlagItem(false, "All milestones are on track"));

        //    if (!hasNotStarted && !hasInProgress)
        //        flags.Add(new RiskFlagItem(false, "All milestones completed"));

        //    return flags;
        //}

        //// ── Phase 3 — Team Timesheets ─────────────────────────────

        //public async Task<TeamTimesheetResponse> GetTeamTimesheetsAsync(int managerEmployeeId, DateOnly weekStart, CancellationToken ct = default)
        //{
        //    // Get all employees currently allocated to any project managed by this manager
        //    var teamEmployeeIds = await context.Allocations
        //        .Where(a => a.Project.ManagerId == managerEmployeeId && a.ToDate >= weekStart)
        //        .Select(a => a.EmployeeId)
        //        .Distinct()
        //        .ToListAsync(ct);

        //    var entries = new List<TeamTimesheetEntryResponse>();

        //    foreach (var employeeId in teamEmployeeIds)
        //    {
        //        var employee = await context.Employees.FindAsync([employeeId], ct);
        //        if (employee is null) continue;

        //        var timesheet = await context.Timesheets
        //            .Include(t => t.Entries)
        //                .ThenInclude(e => e.Project)
        //            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart, ct);

        //        if (timesheet is not null)
        //        {
        //            // Employee submitted — one entry per project
        //            foreach (var entry in timesheet.Entries)
        //            {
        //                entries.Add(new TeamTimesheetEntryResponse(
        //                    employeeId, employee.FullName,
        //                    entry.ProjectId, entry.Project.Name,
        //                    entry.HoursWorked, "Submitted"
        //                ));
        //            }
        //        }
        //        else
        //        {
        //            // Employee did not submit — show as Missed
        //            entries.Add(new TeamTimesheetEntryResponse(
        //                employeeId, employee.FullName, 0, "-", 0, "Missed"
        //            ));
        //        }
        //    }

        //    return new TeamTimesheetResponse(weekStart, entries.OrderBy(e => e.EmployeeName));
        //}

        //public async Task<TeamTimesheetDetailResponse?> GetTeamMemberTimesheetDetailAsync(int employeeId, DateOnly weekStart, CancellationToken ct = default)
        //{
        //    var timesheet = await context.Timesheets
        //        .Include(t => t.Employee)
        //        .Include(t => t.Entries)
        //            .ThenInclude(e => e.Project)
        //        .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart, ct);

        //    if (timesheet is null) return null;

        //    var projectEntries = timesheet.Entries.Select(e => new TeamTimesheetProjectEntryResponse(
        //        e.Project.Name,
        //        e.HoursWorked,
        //        e.ActivityTags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList()
        //    ));

        //    return new TeamTimesheetDetailResponse(
        //        employeeId, timesheet.Employee.FullName,
        //        weekStart, timesheet.TotalHours,
        //        timesheet.Status.ToString(), projectEntries
        //    );
        //}

        //// ── Phase 3 — Allocation helpers ─────────────────────────

        //public async Task<IEnumerable<AllocationOnProjectResponse>> GetActiveAllocationsOnProjectAsync(int projectId, CancellationToken ct = default)
        //{
        //    var today = DateOnly.FromDateTime(DateTime.UtcNow);

        //    return await context.Allocations
        //        .Include(a => a.Employee)
        //        .Where(a => a.ProjectId == projectId && a.ToDate >= today)
        //        .OrderBy(a => a.Employee.FullName)
        //        .Select(a => new AllocationOnProjectResponse(
        //            a.Id, a.EmployeeId, a.Employee.FullName,
        //            a.UtilisationPercentage, a.FromDate, a.ToDate
        //        ))
        //        .ToListAsync(ct);
        //}

        //public async Task<int> GetEmployeeUtilisationInPeriodAsync(
        //    int employeeId, DateOnly from, DateOnly to,
        //    int? excludeAllocationId, CancellationToken ct = default)
        //{
        //    // Two date ranges overlap when: fromA <= toB AND toA >= fromB
        //    var query = context.Allocations
        //        .Where(a => a.EmployeeId == employeeId
        //                 && a.FromDate <= to
        //                 && a.ToDate >= from);

        //    if (excludeAllocationId.HasValue)
        //        query = query.Where(a => a.Id != excludeAllocationId.Value);

        //    return await query.SumAsync(a => a.UtilisationPercentage, ct);
        //}

        //// ── Phase 4 — Employee Timesheets ─────────────────────────

        //public async Task<IEnumerable<ActiveAllocationForSubmitResponse>> GetActiveAllocationsForWeekAsync(
        //    int employeeId, DateOnly weekStart, int maxWeeklyHours, CancellationToken ct = default)
        //{
        //    // A week runs Monday–Sunday. An allocation covers this week if:
        //    // allocation.FromDate <= weekEnd AND allocation.ToDate >= weekStart
        //    var weekEnd = weekStart.AddDays(6);

        //    return await context.Allocations
        //        .Include(a => a.Project)
        //        .Where(a => a.EmployeeId == employeeId
        //                 && a.FromDate <= weekEnd
        //                 && a.ToDate >= weekStart)
        //        .OrderBy(a => a.Project.Name)
        //        .Select(a => new ActiveAllocationForSubmitResponse(
        //            a.ProjectId,
        //            a.Project.Name,
        //            a.UtilisationPercentage,
        //            // Per-project hour cap = allocation% × max weekly hours
        //            a.UtilisationPercentage * maxWeeklyHours / 100
        //        ))
        //        .ToListAsync(ct);
        //}

        //public async Task<IEnumerable<TimesheetSummaryResponse>> GetMyTimesheetsAsync(
        //    int employeeId, CancellationToken ct = default)
        //{
        //    return await context.Timesheets
        //        .Where(t => t.EmployeeId == employeeId)
        //        .OrderByDescending(t => t.WeekStartDate)
        //        .Select(t => new TimesheetSummaryResponse(
        //            t.Id,
        //            t.WeekStartDate,
        //            t.TotalHours,
        //            t.Status.ToString()
        //        ))
        //        .ToListAsync(ct);
        //}

        //public async Task<TimesheetDetailResponse?> GetMyTimesheetDetailAsync(
        //    int employeeId, DateOnly weekStart, CancellationToken ct = default)
        //{
        //    var timesheet = await context.Timesheets
        //        .Include(t => t.Entries)
        //            .ThenInclude(e => e.Project)
        //        .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart, ct);

        //    if (timesheet is null) return null;

        //    var entries = timesheet.Entries.Select(e => new TimesheetEntryResponse(
        //        e.Id,
        //        e.ProjectId,
        //        e.Project.Name,
        //        e.HoursWorked,
        //        e.ActivityTags
        //            .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //            .Select(t => t.Trim())
        //            .Where(t => !string.IsNullOrEmpty(t))
        //            .ToList()
        //    ));

        //    return new TimesheetDetailResponse(
        //        timesheet.Id,
        //        timesheet.WeekStartDate,
        //        timesheet.TotalHours,
        //        timesheet.Status.ToString(),
        //        entries
        //    );
        //}

        //public async Task<MyAllocationsResponse> GetMyAllocationsAsync(
        //    int employeeId, CancellationToken ct = default)
        //{
        //    var today = DateOnly.FromDateTime(DateTime.UtcNow);

        //    var allocations = await context.Allocations
        //        .Include(a => a.Project)
        //        .Where(a => a.EmployeeId == employeeId)
        //        .OrderByDescending(a => a.FromDate)
        //        .Select(a => new MyAllocationResponse(
        //            a.Id,
        //            a.ProjectId,
        //            a.Project.Name,
        //            a.UtilisationPercentage,
        //            a.FromDate,
        //            a.ToDate,
        //            // Active = currently ongoing, Past = already ended
        //            a.ToDate >= today ? "Active" : "Past"
        //        ))
        //        .ToListAsync(ct);

        //    var totalActive = allocations
        //        .Where(a => a.AllocationStatus == "Active")
        //        .Sum(a => a.UtilisationPercentage);

        //    return new MyAllocationsResponse(allocations, totalActive);
        //}

        //// ── Phase 5 stub ──────────────────────────────────────────

        //public async Task<IEnumerable<EmployeeAiContext>> GetEmployeesForSkillMatchAsync(CancellationToken ct = default)
        //{
        //    var today = DateOnly.FromDateTime(DateTime.UtcNow);
        //    var fourWeeksAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-28));

        //    var employees = await context.Employees
        //        .Include(e => e.Skills)
        //        .Include(e => e.Allocations.Where(a => a.FromDate <= today && a.ToDate >= today))
        //        .Include(e => e.Timesheets.Where(t => t.WeekStartDate >= fourWeeksAgo))
        //            .ThenInclude(t => t.Entries)
        //        .Where(e => e.IsActive)
        //        .ToListAsync(ct);

        //    return employees.Select(e =>
        //    {
        //        var totalUtil = e.Allocations.Sum(a => a.UtilisationPercentage);
        //        var recentTags = e.Timesheets
        //            .SelectMany(t => t.Entries)
        //            .Where(en => !string.IsNullOrEmpty(en.ActivityTags))
        //            .SelectMany(en => en.ActivityTags.Split(',', StringSplitOptions.RemoveEmptyEntries))
        //            .Select(t => t.Trim())
        //            .Distinct()
        //            .ToList();

        //        return new EmployeeAiContext(
        //            e.Id, e.FullName, e.Department, e.Designation,
        //            Math.Max(0, 100 - totalUtil),
        //            e.Skills.Select(s => s.SkillName).ToList(),
        //            recentTags
        //        );
        //    });
        //}
    }
}
