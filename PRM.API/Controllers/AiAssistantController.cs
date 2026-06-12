using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs;
using PRM.Application.UseCases.AI;
using PRM.Infrastructure.AI.Prompts;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize(Roles = "Manager")]
    public class AiAssistantController(
    SkillMatchUseCase skillMatch,
    TeamSkillMatchUseCase teamSkillMatch,
    RiskSummaryUseCase riskSummary) : ControllerBase
    {
        /// <summary>
        /// POST /api/ai/skill-match
        /// Finds best-fit employees for a plain-English requirement.
        /// Filters candidates by free capacity before calling AI.
        /// Returns ranked list with AI-generated reasons.
        ///
        /// Body: { "requirementText": "Java developer with microservices", "projectContext": "Alpha Portal" }
        /// </summary>
        [HttpPost("skill-match")]
        public async Task<IActionResult> SkillMatch(
            [FromBody] SkillMatchRequest request,
            CancellationToken ct)
        {
            var result = await skillMatch.ExecuteAsync(request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>
        /// POST /api/ai/team-staffing
        /// Infers roles from a plain-English team requirement and staffs from bench employees.
        ///
        /// Body: { "requirementText": "5-person React + .NET squad with QA for 6 months", "projectContext": "Alpha Portal" }
        /// </summary>
        [HttpPost("team-staffing")]
        public async Task<IActionResult> TeamStaffing(
            [FromBody] TeamStaffingRequest request,
            CancellationToken ct)
        {
            var result = await teamSkillMatch.ExecuteAsync(request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>
        /// GET /api/ai/risk-summary/{projectId}
        /// Generates a plain-English health analysis for a project.
        /// Collects milestone + timesheet data, calls AI, returns paragraph.
        /// </summary>
        [HttpGet("risk-summary/{projectId:int}")]
        public async Task<IActionResult> RiskSummary(int projectId, CancellationToken ct)
        {
            var result = await riskSummary.ExecuteAsync(projectId, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }
    }
}
