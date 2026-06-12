using PRM.Application.DTOs.Manager;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    public class GetTeamTimesheetsUseCase(IQueryService queryService)
    {
        public async Task<Result<TeamTimesheetResponse>> ExecuteAsync(
            int managerEmployeeId,
            DateOnly weekStart,
            CancellationToken ct = default)
        {
            // Normalise to Monday of the given week — consistent with how timesheets are stored
            var monday = ToMonday(weekStart);

            var result = await queryService.GetTeamTimesheetsAsync(managerEmployeeId, monday, ct);
            return Result<TeamTimesheetResponse>.Success(result);
        }

        public async Task<Result<TeamTimesheetDetailResponse>> GetDetailAsync(
            int employeeId,
            DateOnly weekStart,
            CancellationToken ct = default)
        {
            var monday = ToMonday(weekStart);

            var detail = await queryService.GetTeamMemberTimesheetDetailAsync(employeeId, monday, ct);
            if (detail is null)
                return Result<TeamTimesheetDetailResponse>.Failure(
                    $"No timesheet found for employee {employeeId} for week starting {monday}.");

            return Result<TeamTimesheetDetailResponse>.Success(detail);
        }

        /// <summary>
        /// Ensures the date is always the Monday of its week.
        /// Timesheets are keyed on WeekStartDate which is always a Monday.
        /// </summary>
        private static DateOnly ToMonday(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            // DayOfWeek: Sunday=0, Monday=1 ... Saturday=6
            // Adjust so Monday=0
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return date.AddDays(-daysFromMonday);
        }
    }
}
