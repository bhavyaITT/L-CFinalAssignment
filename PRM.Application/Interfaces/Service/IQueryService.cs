using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Employee;
using PRM.Application.DTOs.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.Interfaces.Service
{
    // <summary>
    /// Interface for queries that require eagerly loading related data (Include/Join).
    /// Application layer defines what data shapes it needs.
    /// Infrastructure implements the EF Core queries.
    /// This preserves Dependency Inversion — Application never imports EF Core or AppDbContext.
    /// </summary>
    public interface IQueryService
    {
        // ── Phase 2 queries ───────────────────────────────────────
        Task<EmployeeDetailResponse?> GetEmployeeWithSkillsAsync(int employeeId, CancellationToken ct = default);
        Task<ProjectDetailResponse?> GetProjectWithMilestonesAsync(int projectId, CancellationToken ct = default);
        Task<IEnumerable<ProjectSummaryResponse>> GetAllProjectsWithManagerAsync(CancellationToken ct = default);
        Task<IEnumerable<AllocationResponse>> GetAllocationsAsync(int? employeeId, int? projectId, bool activeOnly, CancellationToken ct = default);

        // ── Phase 3 — Resource Dashboard ─────────────────────────
        /// <summary>All active employees with their current summed utilisation in an active period.</summary>
        //Task<IEnumerable<EmployeeUtilisationRow>> GetActiveEmployeeUtilisationsAsync(CancellationToken ct = default);

        /// <summary>
        /// Recent activity tags for an employee from timesheet entries in the last N weeks.
        /// Powers the "Recent Activity Tags" line in the drill-in view.
        /// </summary>
        //Task<IEnumerable<string>> GetRecentActivityTagsAsync(int employeeId, int weekCount, CancellationToken ct = default);

        /// <summary>Active allocations for a single employee — used in dashboard drill-in.</summary>
        //Task<IEnumerable<ActiveAllocationItem>> GetActiveAllocationsForEmployeeAsync(int employeeId, CancellationToken ct = default);

        // ── Phase 3 — Manager Projects ────────────────────────────
        /// <summary>Projects owned by a specific manager, with milestones and allocated resources.</summary>
        //Task<IEnumerable<ManagerProjectSummaryResponse>> GetManagerProjectsAsync(int managerEmployeeId, CancellationToken ct = default);
        //Task<ManagerProjectDetailResponse?> GetManagerProjectDetailAsync(int projectId, int managerEmployeeId, CancellationToken ct = default);

        // ── Phase 3 — Team Timesheets ─────────────────────────────
        /// <summary>
        /// Timesheets for all employees currently allocated to any project managed by this manager,
        /// for the given week. Returns Missed entries for allocated employees who did not submit.
        /// </summary>
        //Task<TeamTimesheetResponse> GetTeamTimesheetsAsync(int managerEmployeeId, DateOnly weekStart, CancellationToken ct = default);
        //Task<TeamTimesheetDetailResponse?> GetTeamMemberTimesheetDetailAsync(int employeeId, DateOnly weekStart, CancellationToken ct = default);

        // ── Phase 3 — Allocation helpers ─────────────────────────
        /// <summary>All active allocations on a given project (for the End Allocation screen).</summary>
        //Task<IEnumerable<AllocationOnProjectResponse>> GetActiveAllocationsOnProjectAsync(int projectId, CancellationToken ct = default);

        /// <summary>
        /// Sum of utilisation % for an employee across all allocations overlapping
        /// the requested date range. Used for over-allocation validation.
        /// </summary>
        //Task<int> GetEmployeeUtilisationInPeriodAsync(int employeeId, DateOnly from, DateOnly to, int? excludeAllocationId, CancellationToken ct = default);

        // ── Phase 4 — Employee Timesheets ─────────────────────────

        /// <summary>
        /// Active allocations for an employee that overlap the given week.
        /// Used by the Submit Timesheet screen to show which projects
        /// the employee can log hours for and their per-project hour cap.
        /// </summary>
        //Task<IEnumerable<ActiveAllocationForSubmitResponse>> GetActiveAllocationsForWeekAsync(
        //    int employeeId, DateOnly weekStart, int maxWeeklyHours, CancellationToken ct = default);

        /// <summary>
        /// All timesheets for an employee, newest first.
        /// Used for the "View My Timesheets" list screen.
        /// </summary>
        //Task<IEnumerable<TimesheetSummaryResponse>> GetMyTimesheetsAsync(
        //    int employeeId, CancellationToken ct = default);

        /// <summary>
        /// Full detail of one timesheet week including entries with project name and tags.
        /// </summary>
        //Task<TimesheetDetailResponse?> GetMyTimesheetDetailAsync(
        //    int employeeId, DateOnly weekStart, CancellationToken ct = default);

        /// <summary>
        /// All allocations for an employee (active + past) with allocation status label.
        /// Powers the "View My Allocations" screen.
        /// </summary>
        //Task<MyAllocationsResponse> GetMyAllocationsAsync(
        //    int employeeId, CancellationToken ct = default);

        // ── Phase 5 — AI context builders (stubbed here, implemented in Phase 5) ─
        //Task<IEnumerable<EmployeeAiContext>> GetEmployeesForSkillMatchAsync(CancellationToken ct = default);
    }

    // ── Supporting read models used across multiple queries ───────

    /// <summary>Flat row used to build the Resource Dashboard.</summary>
    public record EmployeeUtilisationRow(
        int EmployeeId,
        string FullName,
        string Department,
        string Designation,
        int TotalUtilisationPercent
    );

    /// <summary>Data packed into the AI prompt for skill matching.</summary>
    public record EmployeeAiContext(
        int EmployeeId,
        string FullName,
        string Department,
        string Designation,
        int FreeCapacityPercent,
        IEnumerable<string> ProfileSkills,
        IEnumerable<string> RecentActivityTags
    );
}
