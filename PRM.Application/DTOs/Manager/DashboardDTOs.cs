using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Manager
{
    // ── Resource Dashboard ────────────────────────────────────────

    /// <summary>An employee currently on bench — fully available.</summary>
    public record BenchEmployeeResponse(
        int Id,
        string FullName,
        string Department,
        string Designation,
        IEnumerable<string> Skills     // Top skill names for display
    );

    /// <summary>An actively allocated employee with calculated free capacity.</summary>
    public record ActiveEmployeeResponse(
        int Id,
        string FullName,
        string Department,
        int TotalUtilisationPercent,
        int FreePercent,               // 100 - TotalUtilisationPercent
        string AvailabilityLabel       // "FULL" | "25% free" | "50% free" etc.
    );

    public record ResourceDashboardResponse(
        IEnumerable<BenchEmployeeResponse> BenchEmployees,
        IEnumerable<ActiveEmployeeResponse> ActiveEmployees,
        int BenchCount,
        int OverUtilisedCount,         // > 100% — should not happen but flagged if it does
        int PartialCount               // Allocated but < 100%
    );

    /// <summary>Deep drill-in view for a single employee from the dashboard.</summary>
    public record EmployeeDrillInResponse(
        int Id,
        string FullName,
        string Department,
        string Designation,
        string Status,
        int TotalUtilisationPercent,
        IEnumerable<string> ProfileSkills,
        IEnumerable<ActiveAllocationItem> ActiveAllocations,
        IEnumerable<string> RecentActivityTags  // Last 4 weeks of timesheet activity tags
    );

    public record ActiveAllocationItem(
        int AllocationId,
        string ProjectName,
        int UtilisationPercent,
        DateOnly FromDate,
        DateOnly ToDate
    );

}
