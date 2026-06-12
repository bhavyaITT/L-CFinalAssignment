using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    /// <summary>
    /// Enforces all BRD timesheet validation rules in one atomic operation:
    ///
    /// Rule 1 — Only log hours for allocated projects in that week
    /// Rule 2 — Hours per project ≤ (allocation% × maxWeeklyHours / 100)
    /// Rule 3 — Total hours ≤ maxWeeklyHours (system config)
    /// Rule 4 — No duplicate timesheet for the same week (unique constraint + early check)
    /// Rule 5 — Cannot submit for a future week
    ///
    /// All rules are checked before any data is written (Fail Fast principle).
    /// </summary>
    public class SubmitTimesheetUseCase(
        IUnitOfWork unitOfWork,
        IQueryService queryService)
    {
        public async Task<Result<SubmitTimesheetResponse>> ExecuteAsync(
            int employeeId,
            SubmitTimesheetRequest request,
            CancellationToken ct = default)
        {
            var monday = ToMonday(request.WeekStartDate);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // ── Rule 5: No future weeks ───────────────────────────
            if (monday > today)
                return Result<SubmitTimesheetResponse>.Failure(
                    "You cannot submit a timesheet for a future week.");

            // ── Rule 4: No duplicate ──────────────────────────────
            var alreadyExists = await unitOfWork.Timesheets.AnyAsync(
                t => t.EmployeeId == employeeId && t.WeekStartDate == monday, ct);

            if (alreadyExists)
                return Result<SubmitTimesheetResponse>.Failure(
                    $"A timesheet for the week starting {monday:dd-MM-yyyy} has already been submitted.");

            // ── Load system config for hour caps ──────────────────
            var config = (await unitOfWork.SystemConfigurations.GetAllAsync(ct)).FirstOrDefault();
            var maxWeeklyHours = config?.MaxWeeklyHours ?? 40;

            // ── Load employee's active allocations for this week ──
            var activeAllocations = (await queryService
                .GetActiveAllocationsForWeekAsync(employeeId, monday, maxWeeklyHours, ct))
                .ToDictionary(a => a.ProjectId);

            if (!activeAllocations.Any())
                return Result<SubmitTimesheetResponse>.Failure(
                    "You have no active allocations for this week. Contact your manager.");

            // ── Validate each entry ───────────────────────────────
            var entries = request.Entries.ToList();

            if (!entries.Any())
                return Result<SubmitTimesheetResponse>.Failure(
                    "At least one timesheet entry is required.");

            // Check for duplicate project entries in the same request
            var projectIds = entries.Select(e => e.ProjectId).ToList();
            if (projectIds.Count != projectIds.Distinct().Count())
                return Result<SubmitTimesheetResponse>.Failure(
                    "Duplicate project entries found. Each project can only appear once per timesheet.");

            foreach (var entry in entries)
            {
                // Rule 1: Can only log for allocated projects
                if (!activeAllocations.TryGetValue(entry.ProjectId, out var allocation))
                    return Result<SubmitTimesheetResponse>.Failure(
                        $"Project ID {entry.ProjectId} is not in your active allocations for this week.");

                if (entry.HoursWorked < 0)
                    return Result<SubmitTimesheetResponse>.Failure(
                        $"Hours worked cannot be negative for project '{allocation.ProjectName}'.");

                // Rule 2: Per-project hour cap
                if (entry.HoursWorked > allocation.MaxHoursForProject)
                    return Result<SubmitTimesheetResponse>.Failure(
                        $"Hours for '{allocation.ProjectName}' cannot exceed {allocation.MaxHoursForProject} hrs " +
                        $"({allocation.UtilisationPercentage}% of {maxWeeklyHours} hrs max). " +
                        $"You entered {entry.HoursWorked} hrs.");
            }

            // Rule 3: Total hours cap
            var totalHours = entries.Sum(e => e.HoursWorked);
            if (totalHours > maxWeeklyHours)
                return Result<SubmitTimesheetResponse>.Failure(
                    $"Total hours ({totalHours} hrs) exceeds the system maximum of {maxWeeklyHours} hrs per week.");

            // ── All rules passed — persist ────────────────────────
            var timesheet = new Timesheet
            {
                EmployeeId = employeeId,
                WeekStartDate = monday,
                TotalHours = totalHours,
                Status = TimesheetStatus.Submitted
            };

            await unitOfWork.Timesheets.AddAsync(timesheet, ct);

            // Save the header first so we have TimesheetId for the entries
            await unitOfWork.SaveChangesAsync(ct);

            foreach (var entry in entries)
            {
                var timesheetEntry = new TimesheetEntry
                {
                    TimesheetId = timesheet.Id,
                    ProjectId = entry.ProjectId,
                    HoursWorked = entry.HoursWorked,
                    ActivityTags = entry.ActivityTags.Trim()
                };
                await unitOfWork.TimesheetEntries.AddAsync(timesheetEntry, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);

            // Build response with project names from the allocation lookup
            var entryResponses = entries.Select(e =>
            {
                var alloc = activeAllocations[e.ProjectId];
                var tags = e.ActivityTags
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                return new TimesheetEntryResponse(
                    EntryId: 0,       // Will be populated after re-query if needed
                    ProjectId: e.ProjectId,
                    ProjectName: alloc.ProjectName,
                    HoursWorked: e.HoursWorked,
                    ActivityTags: tags
                );
            });

            return Result<SubmitTimesheetResponse>.Success(new SubmitTimesheetResponse(
                TimesheetId: timesheet.Id,
                WeekStartDate: monday,
                TotalHours: totalHours,
                MaxWeeklyHours: maxWeeklyHours,
                Status: timesheet.Status.ToString(),
                Entries: entryResponses
            ));
        }

        private static DateOnly ToMonday(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return date.AddDays(-daysFromMonday);
        }
    }
}
