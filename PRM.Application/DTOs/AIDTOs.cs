using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs
{
    // ── Skill Match ───────────────────────────────────────────────

    public record SkillMatchRequest(
        /// <summary>Plain-English requirement from the manager, e.g. "Java developer with microservices"</summary>
        string RequirementText,

        /// <summary>
        /// Optional project context. Helps AI tailor the explanation.
        /// e.g. "Alpha Portal — backend modernisation, 3 month engagement"
        /// </summary>
        string? ProjectContext = null
    );

    public record SkillMatchCandidateResponse(
        int EmployeeId,
        string FullName,
        string Department,
        string Designation,
        int FreeCapacityPercent,
        IEnumerable<string> ProfileSkills,
        IEnumerable<string> RecentActivityTags,

        /// <summary>AI-generated plain-English reason for the match.</summary>
        string AiReason,

        /// <summary>
        /// Suggested allocation % when manager specifies a part-time hours need.
        /// Null for full-time / open-ended requests.
        /// </summary>
        int? SuggestedAllocationPercent
    );

    public record SkillMatchResponse(
        string RequirementText,
        IEnumerable<SkillMatchCandidateResponse> Candidates,
        int TotalCandidatesEvaluated,
        string AiNote      // "Results are AI-generated. Verify before confirming."
    );

    // ── Risk Summary ──────────────────────────────────────────────

    public record RiskSummaryRequest(
        int ProjectId
    );

    public record RiskSummaryResponse(
        int ProjectId,
        string ProjectName,
        string Health,
        /// <summary>The AI-generated plain-English paragraph.</summary>
        string Summary,
        string AiNote      // "AI-generated from milestone and timesheet data."
    );

    // ── Team Staffing (bench-only, multi-role) ────────────────────

    public record TeamStaffingRequest(
        /// <summary>
        /// Plain-English team need, e.g. "Need a 5-person squad for a React + .NET microservices project
        /// with QA and a scrum master for 6 months."
        /// </summary>
        string RequirementText,
        string? ProjectContext = null
    );

    public record TeamRoleMatchResponse(
        string RoleTitle,
        int EmployeeId,
        string EmployeeName,
        string MatchReason
    );

    public record TeamRoleGapResponse(
        string RoleTitle,
        string GapType,              // SkillGap | AvailabilityGap
        string Reason,
        DateOnly? NextAvailableDate,
        string? AllocatedEmployeeName
    );

    public record TeamStaffingResponse(
        string RequirementText,
        string? ProjectContext,
        IEnumerable<TeamRoleMatchResponse> Matches,
        IEnumerable<TeamRoleGapResponse> Gaps,
        int BenchCandidatesConsidered,
        string AiNote
    );
}
