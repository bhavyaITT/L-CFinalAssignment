using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    public class GetMyTimesheetsUseCase(IQueryService queryService)
    {
        public async Task<Result<MyTimesheetsResponse>> ExecuteAsync(
            int employeeId, CancellationToken ct = default)
        {
            var timesheets = await queryService.GetMyTimesheetsAsync(employeeId, ct);

            return Result<MyTimesheetsResponse>.Success(
                new MyTimesheetsResponse(timesheets));
        }

        public async Task<Result<TimesheetDetailResponse>> GetDetailAsync(
            int employeeId, DateOnly weekStart, CancellationToken ct = default)
        {
            var monday = ToMonday(weekStart);

            var detail = await queryService.GetMyTimesheetDetailAsync(employeeId, monday, ct);
            if (detail is null)
                return Result<TimesheetDetailResponse>.Failure(
                    $"No timesheet found for the week starting {monday:dd-MM-yyyy}.");

            return Result<TimesheetDetailResponse>.Success(detail);
        }

        private static DateOnly ToMonday(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return date.AddDays(-daysFromMonday);
        }
    }

}
