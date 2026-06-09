using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Project;
using PRM.Application.UseCases.Projects;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/projects")]
    [Authorize(Roles = "Admin")]
    public class ProjectsController(
        CreateProjectUseCase createProject,
        GetAllProjectsUseCase getAllProjects,
        GetProjectDetailUseCase getProjectDetail,
        UpdateProjectUseCase updateProject,
        AddMilestoneUseCase addMilestone,
        UpdateMilestoneStatusUseCase updateMilestoneStatus) : ControllerBase
    {
        /// <summary>GET /api/projects</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await getAllProjects.ExecuteAsync(ct);
            return Ok(result.Data);
        }

        /// <summary>GET /api/projects/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await getProjectDetail.ExecuteAsync(id, ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>POST /api/projects</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
        {
            var result = await createProject.ExecuteAsync(request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
        }

        /// <summary>PUT /api/projects/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
        {
            var result = await updateProject.ExecuteAsync(id, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        // ── Milestones ────────────────────────────────────────────────────────────

        /// <summary>POST /api/projects/{id}/milestones</summary>
        [HttpPost("{id:int}/milestones")]
        public async Task<IActionResult> AddMilestone(int id, [FromBody] AddMilestoneRequest request, CancellationToken ct)
        {
            var result = await addMilestone.ExecuteAsync(id, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return CreatedAtAction(nameof(GetById), new { id }, result.Data);
        }

        /// <summary>PUT /api/projects/milestones/{milestoneId}</summary>
        [HttpPut("milestones/{milestoneId:int}")]
        public async Task<IActionResult> UpdateMilestoneStatus(int milestoneId, [FromBody] UpdateMilestoneStatusRequest request, CancellationToken ct)
        {
            var result = await updateMilestoneStatus.ExecuteAsync(milestoneId, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Milestone status updated." });
        }
    }
}
