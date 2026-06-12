using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    /// <summary>
    /// Checks if the employee missed submitting their timesheet for the most
    /// recently completed week (i.e. last Monday–Sunday block).
    ///
    /// BRD: "The reminder is shown only if a timesheet is missing for the most
    /// recent completed week. It disappears once submitted."
    ///
    /// Logic:
    /// - "Most recently completed week" = the Monday-to-Sunday week that ended
    ///   before today's Monday. If today IS Monday, last week ended yesterday.
    /// - A week is "missed" if the employee had at least one active allocation
    ///   in that week AND no timesheet record exists for it.
    /// - We do NOT flag a week as missed if the employee had no allocations —
    ///   they had nothing to log.
    /// </summary>
    public class CheckMissedTimesheetUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<MissedTimesheetReminderResponse>> ExecuteAsync(
            int employeeId, CancellationToken ct = default)
        {
            var lastMonday = GetLastCompletedWeekMonday();
            var lastSunday = lastMonday.AddDays(6);

            // Check if the employee had any active allocations in that week
            var hadAllocations = await unitOfWork.Allocations.AnyAsync(
                a => a.EmployeeId == employeeId
                  && a.FromDate <= lastSunday
                  && a.ToDate >= lastMonday,
                ct);

            // No allocations that week → no reminder needed
            if (!hadAllocations)
                return Result<MissedTimesheetReminderResponse>.Success(
                    new MissedTimesheetReminderResponse(IsMissing: false, MissingWeekStart: null));

            // Check if a timesheet was already submitted for that week
            var hasTimesheet = await unitOfWork.Timesheets.AnyAsync(
                t => t.EmployeeId == employeeId && t.WeekStartDate == lastMonday,
                ct);

            return Result<MissedTimesheetReminderResponse>.Success(
                new MissedTimesheetReminderResponse(
                    IsMissing: !hasTimesheet,
                    MissingWeekStart: !hasTimesheet ? lastMonday : null
                ));
        }

        /// <summary>
        /// Returns the Monday of the most recently completed week.
        /// e.g. If today is Wednesday 14-May-2026, returns Monday 06-May-2026.
        ///      If today is Monday 12-May-2026, returns Monday 05-May-2026.
        /// </summary>
        private static DateOnly GetLastCompletedWeekMonday()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dayOfWeek = (int)today.DayOfWeek;
            // Days since last Monday (Sunday = 7 days back, Monday = 7 days back too)
            var daysToLastMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            var thisMonday = today.AddDays(-daysToLastMonday);
            return thisMonday.AddDays(-7);  // Go back one more week
        }
    }
}
