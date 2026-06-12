using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Employee;
using PRM.Application.UseCases.Employees;
using PRM.Application.UseCases.Manager;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/employee")]
    [Authorize(Roles = "Employee")]
    public class EmployeeTimesheetsController(
    ResolveEmployeeIdUseCase resolveEmployeeId,
    GetActiveAllocationsForWeekUseCase getAllocationsForWeek,
    SubmitTimesheetUseCase submitTimesheet,
    GetMyTimesheetsUseCase getMyTimesheets,
    GetMyAllocationsUseCase getMyAllocations,
    CheckMissedTimesheetUseCase checkMissed) : ControllerBase
    {
        // ── Shared helper ─────────────────────────────────────────

        private async Task<(IActionResult? error, int employeeId)> GetEmployeeIdAsync(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId is null) return (Unauthorized(), 0);

            var result = await resolveEmployeeId.ExecuteAsync(userId.Value, ct);
            if (!result.IsSuccess)
                return (BadRequest(new { message = result.Error }), 0);

            return (null, result.Data);
        }

        // ── Missed timesheet reminder ─────────────────────────────

        /// <summary>
        /// GET /api/employee/timesheet-reminder
        /// Called on Employee home screen load.
        /// Returns IsMissing=true + the missing week if last week's timesheet was not submitted.
        /// </summary>
        [HttpGet("timesheet-reminder")]
        public async Task<IActionResult> GetReminder(CancellationToken ct)
        {
            var (err, employeeId) = await GetEmployeeIdAsync(ct);
            if (err is not null) return err;

            var result = await checkMissed.ExecuteAsync(employeeId, ct);
            return Ok(result.Data);
        }

        // ── Submit Timesheet ──────────────────────────────────────

        /// <summary>
        /// GET /api/employee/timesheets/allocations?weekStart=2026-05-12
        /// Pre-submit: returns which projects the employee can log hours for
        /// and each project's per-week hour cap.
        /// Called before the Submit form is shown so the UI can render the project list.
        /// </summary>
        [HttpGet("timesheets/allocations")]
        public async Task<IActionResult> GetAllocationsForWeek(
            [FromQuery] DateOnly? weekStart,
            CancellationToken ct)
        {
            var (err, employeeId) = await GetEmployeeIdAsync(ct);
            if (err is not null) return err;

            var week = weekStart ?? GetCurrentMonday();
            var result = await getAllocationsForWeek.ExecuteAsync(employeeId, week, ct);
            return Ok(result.Data);
        }

        /// <summary>
        /// POST /api/employee/timesheets
        /// Submit a weekly timesheet. All 5 BRD validation rules enforced server-side.
        /// </summary>
        [HttpPost("timesheets")]
        public async Task<IActionResult> Submit(
            [FromBody] SubmitTimesheetRequest request,
            CancellationToken ct)
        {
            var (err, employeeId) = await GetEmployeeIdAsync(ct);
            if (err is not null) return err;

            var result = await submitTimesheet.ExecuteAsync(employeeId, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return CreatedAtAction(nameof(GetDetail),
                new { weekStart = result.Data!.WeekStartDate },
                result.Data);
        }

        // ── View My Timesheets ────────────────────────────────────

        /// <summary>
        /// GET /api/employee/timesheets
        /// List of all timesheets, newest first. Shows Submitted / Missed status.
        /// </summary>
        [HttpGet("timesheets")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var (err, employeeId) = await GetEmployeeIdAsync(ct);
            if (err is not null) return err;

            var result = await getMyTimesheets.ExecuteAsync(employeeId, ct);
            return Ok(result.Data);
        }

        /// <summary>
        /// GET /api/employee/timesheets/{weekStart}
        /// Week detail: project entries with hours and activity tags.
        /// weekStart format: yyyy-MM-dd (e.g. 2026-05-12)
        /// </summary>
        [HttpGet("timesheets/{weekStart}")]
        public async Task<IActionResult> GetDetail(DateOnly weekStart, CancellationToken ct)
        {
            var (err, employeeId) = await GetEmployeeIdAsync(ct);
            if (err is not null) return err;

            var result = await getMyTimesheets.GetDetailAsync(employeeId, weekStart, ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }

        // ── View My Allocations ───────────────────────────────────

        /// <summary>
        /// GET /api/employee/allocations
        /// All allocations (active and past) with total current utilisation.
        /// </summary>
        [HttpGet("allocations")]
        public async Task<IActionResult> GetAllocations(CancellationToken ct)
        {
            var (err, employeeId) = await GetEmployeeIdAsync(ct);
            if (err is not null) return err;

            var result = await getMyAllocations.ExecuteAsync(employeeId, ct);
            return Ok(result.Data);
        }

        // ── Helpers ───────────────────────────────────────────────

        private static DateOnly GetCurrentMonday()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dayOfWeek = (int)today.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return today.AddDays(-daysFromMonday);
        }
    }
    // Add this helper extension method to extract the user ID from ClaimsPrincipal
    //public static class ClaimsPrincipalExtensions
    //{
    //    public static int? GetUserId(this ClaimsPrincipal user)
    //    {
    //        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
    //        if (userIdClaim == null) return null;
    //        if (int.TryParse(userIdClaim.Value, out var id))
    //            return id;
    //        return null;
    //    }
    //}
}
