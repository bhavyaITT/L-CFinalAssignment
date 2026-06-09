using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Project
{
    // ── Requests ──────────────────────────────────────────────────

    public record CreateProjectRequest(
        string Name,
        string Description,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status,     // "Planned" | "Active" | "OnHold"
        int ManagerId
    );

    public record UpdateProjectRequest(
        string Name,
        string Description,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status,
        int ManagerId
    );

    public record AddMilestoneRequest(
        string Title,
        DateOnly DueDate
    );

    public record UpdateMilestoneStatusRequest(
        string Status    // "NotStarted" | "InProgress" | "Done"
    );

    // ── Responses ─────────────────────────────────────────────────

    public record MilestoneResponse(
        int Id,
        string Title,
        DateOnly DueDate,
        string Status
    );

    public record ProjectSummaryResponse(
        int Id,
        string Name,
        string Description,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status,
        string Health,
        int ManagerId,
        string ManagerName
    );

    public record ProjectDetailResponse(
        int Id,
        string Name,
        string Description,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status,
        string Health,
        int ManagerId,
        string ManagerName,
        IEnumerable<MilestoneResponse> Milestones
    );

    public record ProjectsListResponse(
        IEnumerable<ProjectSummaryResponse> Projects,
        int TotalCount
    );

}
