using PRM.Application.DTOs.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Manager
{
    // ── My Projects ───────────────────────────────────────────────

    /// <summary>Single risk flag line shown under a project (e.g. "Backend API milestone is 5 days overdue").</summary>
    public record RiskFlagItem(
        bool IsCritical,   // true = ✗, false = ✓
        string Message
    );

    public record ManagerProjectDetailResponse(
        int Id,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status,
        string Health,
        IEnumerable<RiskFlagItem> RiskFlags,
        IEnumerable<MilestoneResponse> Milestones,
        IEnumerable<AllocationOnProjectResponse> AllocatedResources
    );

    public record ManagerProjectSummaryResponse(
        int Id,
        string Name,
        DateOnly EndDate,
        string Health        // "OnTrack" | "Attention" | "AtRisk"
    );

    // ── Timesheets (Manager Team View) ───────────────────────────

    public record TeamTimesheetEntryResponse(
        int EmployeeId,
        string EmployeeName,
        int ProjectId,
        string ProjectName,
        int HoursWorked,
        string Status       // "Submitted" | "Missed"
    );

    public record TeamTimesheetResponse(
        DateOnly WeekStartDate,
        IEnumerable<TeamTimesheetEntryResponse> Entries
    );

    public record TeamTimesheetDetailResponse(
        int EmployeeId,
        string EmployeeName,
        DateOnly WeekStartDate,
        int TotalHours,
        string Status,
        IEnumerable<TeamTimesheetProjectEntryResponse> Projects
    );

    public record TeamTimesheetProjectEntryResponse(
        string ProjectName,
        int HoursWorked,
        IEnumerable<string> ActivityTags
    );
}
