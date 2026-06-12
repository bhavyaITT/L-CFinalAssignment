using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.UseCases;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/allocations")]
    [Authorize(Roles = "Admin,Manager")]  // Admin views all; Manager will filter by their projects in Phase 3
    public class AllocationsController(GetAllAllocationsUseCase getAllAllocations) : ControllerBase
    {
        /// <summary>
        /// GET /api/allocations?employeeId=1&projectId=2&activeOnly=true
        /// Admin sees all. Manager scope enforcement will be added in Phase 3.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? employeeId,
            [FromQuery] int? projectId,
            [FromQuery] bool activeOnly = true,
            CancellationToken ct = default)
        {
            var result = await getAllAllocations.ExecuteAsync(employeeId, projectId, activeOnly, ct);
            return Ok(result.Data);
        }
    }
}
