using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Manager;
using PRM.Application.UseCases.Manager;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/manager/allocations")]
    [Authorize(Roles = "Manager")]
    public class ManagerAllocationsController(
    ValidateAllocationUseCase validateAllocation,
    CreateAllocationUseCase createAllocation,
    EndAllocationUseCase endAllocation,
    GetActiveAllocationsOnProjectUseCase getOnProject,
    ResolveEmployeeIdUseCase resolveEmployeeId) : ControllerBase
    {
        /// <summary>
        /// POST /api/manager/allocations/validate
        /// Step 1: Preview — shows "Validating..." line before the manager confirms.
        /// Does NOT save anything. Returns IsValid + human-readable messagae.
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] CreateAllocationRequest request, CancellationToken ct)
        {
            var result = await validateAllocation.ExecuteAsync(request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>
        /// POST /api/manager/allocations
        /// Step 2: Confirm — saves the allocation after manager has seen validation.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAllocationRequest request, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var employeeIdResult = await resolveEmployeeId.ExecuteAsync(userId.Value, ct);
            if (!employeeIdResult.IsSuccess)
                return BadRequest(new { message = employeeIdResult.Error });

            var result = await createAllocation.ExecuteAsync(request, employeeIdResult.Data, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return CreatedAtAction(nameof(GetOnProject),
                new { projectId = request.ProjectId }, result.Data);
        }

        /// <summary>
        /// GET /api/manager/allocations/projects/{projectId}
        /// List active allocations on a project — used for the End Allocation screen.
        /// </summary>
        [HttpGet("projects/{projectId:int}")]
        public async Task<IActionResult> GetOnProject(int projectId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var employeeIdResult = await resolveEmployeeId.ExecuteAsync(userId.Value, ct);
            if (!employeeIdResult.IsSuccess)
                return BadRequest(new { message = employeeIdResult.Error });

            var result = await getOnProject.ExecuteAsync(projectId, employeeIdResult.Data, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>
        /// DELETE /api/manager/allocations/{id}
        /// End an allocation — sets ToDate to today, recomputes employee status.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> End(int id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var employeeIdResult = await resolveEmployeeId.ExecuteAsync(userId.Value, ct);
            if (!employeeIdResult.IsSuccess)
                return BadRequest(new { message = employeeIdResult.Error });

            var result = await endAllocation.ExecuteAsync(id, employeeIdResult.Data, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }
    }
}
