using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    /// <summary>
    /// Returns the projects an employee can log hours for in a given week,
    /// together with their per-project hour cap.
    /// This powers the Submit Timesheet screen's project list.
    /// </summary>
    public class GetActiveAllocationsForWeekUseCase(
        IUnitOfWork unitOfWork,
        IQueryService queryService)
    {
        public async Task<Result<ActiveAllocationsForWeekResponse>> ExecuteAsync(
            int employeeId,
            DateOnly weekStart,
            CancellationToken ct = default)
        {
            var monday = ToMonday(weekStart);

            // Fetch system max weekly hours — drives per-project cap calculation
            var config = (await unitOfWork.SystemConfigurations.GetAllAsync(ct)).FirstOrDefault();
            var maxWeeklyHours = config?.MaxWeeklyHours ?? 40;

            var allocations = await queryService.GetActiveAllocationsForWeekAsync(
                employeeId, monday, maxWeeklyHours, ct);

            return Result<ActiveAllocationsForWeekResponse>.Success(new ActiveAllocationsForWeekResponse(
                WeekStartDate: monday,
                MaxWeeklyHours: maxWeeklyHours,
                Allocations: allocations
            ));
        }

        private static DateOnly ToMonday(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return date.AddDays(-daysFromMonday);
        }
    }
}
