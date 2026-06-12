using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.UseCases.Manager;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/manager/dashboard")]
    [Authorize(Roles = "Manager")]
    public class ResourceDashboardController(
    GetResourceDashboardUseCase getDashboard,
    GetEmployeeDrillInUseCase getDrillIn,
    ResolveEmployeeIdUseCase resolveEmployeeId) : ControllerBase
    {
        /// <summary>
        /// GET /api/manager/dashboard
        /// Returns bench list, active list with utilisation, and summary counts.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = await getDashboard.ExecuteAsync(ct);
            return Ok(result.Data);
        }

        /// <summary>
        /// GET /api/manager/dashboard/employees/{id}
        /// Drill-in: full profile, active allocations, recent activity tags for one employee.
        /// </summary>
        [HttpGet("employees/{id:int}")]
        public async Task<IActionResult> DrillIn(int id, CancellationToken ct)
        {
            var result = await getDrillIn.ExecuteAsync(id, ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }
    }
}
