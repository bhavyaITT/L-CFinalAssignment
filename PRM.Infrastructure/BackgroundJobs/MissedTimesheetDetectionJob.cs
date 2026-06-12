using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Job 3: Detects employees who were allocated last week but did not submit a timesheet.
    /// Creates a Timesheet record with Status = Missed so the history is complete.
    ///
    /// Run logic: only run once per week (Monday). If a Missed record already exists,
    /// skip to keep the operation idempotent (safe to run multiple times without
    /// creating duplicate records — idempotency is a critical property for scheduled jobs).
    /// </summary>
    public class MissedTimesheetDetectionJob(PRMTDbContext context, ILogger<MissedTimesheetDetectionJob> logger)
    {
        public async Task ExecuteAsync()
        {
            logger.LogInformation("Starting missed timesheet detection at {Time}", DateTime.UtcNow);

            var lastMonday = GetLastMonday();
            var lastSunday = lastMonday.AddDays(6);

            // Find all employees who had at least one active allocation last week
            var allocatedEmployeeIds = await context.Allocations
                .Where(a => a.FromDate <= lastSunday && a.ToDate >= lastMonday)
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToListAsync();

            // Find which ones already submitted
            var submittedEmployeeIds = await context.Timesheets
                .Where(t => t.WeekStartDate == lastMonday
                         && allocatedEmployeeIds.Contains(t.EmployeeId))
                .Select(t => t.EmployeeId)
                .ToListAsync();

            var missedEmployeeIds = allocatedEmployeeIds
                .Except(submittedEmployeeIds)
                .ToList();

            if (!missedEmployeeIds.Any())
            {
                logger.LogInformation("No missed timesheets detected for week {Week}", lastMonday);
                return;
            }

            foreach (var employeeId in missedEmployeeIds)
            {
                // Idempotency guard: skip if a Missed record already exists
                var alreadyFlagged = await context.Timesheets.AnyAsync(t =>
                    t.EmployeeId == employeeId
                    && t.WeekStartDate == lastMonday
                    && t.Status == TimesheetStatus.Missed);

                if (alreadyFlagged) continue;

                context.Timesheets.Add(new Timesheet
                {
                    EmployeeId = employeeId,
                    WeekStartDate = lastMonday,
                    TotalHours = 0,
                    Status = TimesheetStatus.Missed
                });

                logger.LogWarning("Missed timesheet flagged: EmployeeId={Id} for week {Week}",
                    employeeId, lastMonday);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Missed timesheet detection complete. Flagged {Count} employees", missedEmployeeIds.Count);
        }

        private static DateOnly GetLastMonday()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dayOfWeek = (int)today.DayOfWeek;
            var daysToThisMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return today.AddDays(-daysToThisMonday - 7);
        }
    }

}
