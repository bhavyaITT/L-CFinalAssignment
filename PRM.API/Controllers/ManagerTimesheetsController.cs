using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.UseCases.Manager;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/manager/timesheets")]
    [Authorize(Roles = "Manager")]
    public class ManagerTimesheetsController(
    GetTeamTimesheetsUseCase getTeamTimesheets,
    ResolveEmployeeIdUseCase resolveEmployeeId) : ControllerBase
    {
        /// <summary>
        /// GET /api/manager/timesheets?weekStart=2026-05-12
        /// Returns all team timesheet entries for the given week.
        /// Employees who did not submit show as Missed.
        /// weekStart defaults to the current Monday if not provided.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTeamTimesheets(
            [FromQuery] DateOnly? weekStart,
            CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return Unauthorized();

            var employeeIdResult = await resolveEmployeeId.ExecuteAsync(userId.Value, ct);
            if (!employeeIdResult.IsSuccess)
                return BadRequest(new { message = employeeIdResult.Error });

            // Default to current week's Monday if not provided
            var week = weekStart ?? GetCurrentMonday();

            var result = await getTeamTimesheets.ExecuteAsync(employeeIdResult.Data, week, ct);
            return Ok(result.Data);
        }

        /// <summary>
        /// GET /api/manager/timesheets/employees/{employeeId}?weekStart=2026-05-12
        /// Drill into a single employee's timesheet for a specific week.
        /// </summary>
        [HttpGet("employees/{employeeId:int}")]
        public async Task<IActionResult> GetEmployeeDetail(
            int employeeId,
            [FromQuery] DateOnly weekStart,
            CancellationToken ct)
        {
            var result = await getTeamTimesheets.GetDetailAsync(employeeId, weekStart, ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }

        private static DateOnly GetCurrentMonday()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dayOfWeek = (int)today.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return today.AddDays(-daysFromMonday);
        }
    }
}
