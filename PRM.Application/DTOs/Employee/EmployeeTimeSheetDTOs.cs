using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Employee
{
    // ── Submit Timesheet ──────────────────────────────────────────

    /// <summary>
    /// One entry per project within a week. The employee supplies hours and
    /// activity tags for each project they are allocated to.
    /// </summary>
    public record TimesheetEntryRequest(
        int ProjectId,
        int HoursWorked,

        /// <summary>
        /// Comma-separated activity tags chosen by the employee.
        /// e.g. "Microservices / Architecture,WebSocket / Real-time Features"
        /// These directly power the AI Skill Matcher in Phase 5.
        /// </summary>
        string ActivityTags
    );

    public record SubmitTimesheetRequest(
        /// <summary>Always send as the Monday of the relevant week.</summary>
        DateOnly WeekStartDate,
        IEnumerable<TimesheetEntryRequest> Entries
    );

    public record SubmitTimesheetResponse(
        int TimesheetId,
        DateOnly WeekStartDate,
        int TotalHours,
        int MaxWeeklyHours,
        string Status,
        IEnumerable<TimesheetEntryResponse> Entries
    );

    // ── View My Timesheets ────────────────────────────────────────

    public record TimesheetSummaryResponse(
        int TimesheetId,
        DateOnly WeekStartDate,
        int TotalHours,
        string Status    // "Submitted" | "Missed"
    );

    public record TimesheetEntryResponse(
        int EntryId,
        int ProjectId,
        string ProjectName,
        int HoursWorked,
        IEnumerable<string> ActivityTags
    );

    public record TimesheetDetailResponse(
        int TimesheetId,
        DateOnly WeekStartDate,
        int TotalHours,
        string Status,
        IEnumerable<TimesheetEntryResponse> Entries
    );

    public record MyTimesheetsResponse(
        IEnumerable<TimesheetSummaryResponse> Timesheets
    );

    // ── View My Allocations ───────────────────────────────────────

    public record MyAllocationResponse(
        int AllocationId,
        int ProjectId,
        string ProjectName,
        int UtilisationPercentage,
        DateOnly FromDate,
        DateOnly ToDate,

        /// <summary>"Active" if ToDate >= today, "Past" otherwise.</summary>
        string AllocationStatus
    );

    public record MyAllocationsResponse(
        IEnumerable<MyAllocationResponse> Allocations,
        int TotalUtilisationPercent    // Sum across currently active allocations only
    );

    // ── Missed Timesheet Reminder ─────────────────────────────────

    /// <summary>
    /// Returned on the Employee home screen check.
    /// IsMissing = true shows the ⚠ reminder banner.
    /// </summary>
    public record MissedTimesheetReminderResponse(
        bool IsMissing,
        DateOnly? MissingWeekStart    // null when IsMissing = false
    );

    // ── Active Allocations for Submit screen ──────────────────────

    /// <summary>
    /// Shown on the Submit Timesheet screen so the employee knows which
    /// projects they can log hours for and what the per-project hour cap is.
    /// </summary>
    public record ActiveAllocationForSubmitResponse(
        int ProjectId,
        string ProjectName,
        int UtilisationPercentage,
        int MaxHoursForProject    // = allocationPct * maxWeeklyHours / 100
    );

    public record ActiveAllocationsForWeekResponse(
        DateOnly WeekStartDate,
        int MaxWeeklyHours,
        IEnumerable<ActiveAllocationForSubmitResponse> Allocations
    );
}
