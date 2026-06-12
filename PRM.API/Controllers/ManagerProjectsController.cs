using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.UseCases.Manager;
using PRM.Application.UseCases.Projects;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/manager/projects")]
    [Authorize(Roles = "Manager")]
    public class ManagerProjectsController(
    GetMyProjectsUseCase getMyProjects,
    GetMyProjectDetailUseCase getMyProjectDetail,
    ResolveEmployeeIdUseCase resolveEmployeeId) : ControllerBase
    {
        /// <summary>
        /// GET /api/manager/projects
        /// Returns the projects this manager owns, with Health status for each.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var employeeIdResult = await resolveEmployeeId.ExecuteAsync(userId.Value, ct);
            if (!employeeIdResult.IsSuccess)
                return BadRequest(new { message = employeeIdResult.Error });

            var result = await getMyProjects.ExecuteAsync(employeeIdResult.Data, ct);
            return Ok(result.Data);
        }

        /// <summary>
        /// GET /api/manager/projects/{id}
        /// Full project detail: health flags, milestones, allocated resources.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var employeeIdResult = await resolveEmployeeId.ExecuteAsync(userId.Value, ct);
            if (!employeeIdResult.IsSuccess)
                return BadRequest(new { message = employeeIdResult.Error });

            var result = await getMyProjectDetail.ExecuteAsync(id, employeeIdResult.Data, ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }
    }



}
