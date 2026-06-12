using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    public class GetAllEmployeesUseCase(IUnitOfWork unitOfWork)
    {
        /// <param name="statusFilter">Optional: "Bench" or "Allocated"</param>
        /// <param name="departmentFilter">Optional: partial match on department name</param>
        public async Task<Result<EmployeesListResponse>> ExecuteAsync(CancellationToken ct = default)
        {
            var employees = (await unitOfWork.Employees.GetAllAsync(ct)).ToList();
            var employeeIds = employees.Select(e => e.Id).ToHashSet();

            var users = (await unitOfWork.Users.FindAsync(
                u => employeeIds.Contains(u.Id) && u.Role == UserRole.Employee, ct))
                .ToDictionary(u => u.Id);

            var result = employees
                .Where(e => users.ContainsKey(e.Id))
                .OrderBy(e => users[e.Id].FullName)
                .Select(e => MapToResponse(e, users[e.Id]))
                .ToList();

            return Result<EmployeesListResponse>.Success(new EmployeesListResponse(
                Employees: result,
                TotalCount: result.Count,
                AllocatedCount: result.Count(e => e.Status.Equals("Allocated", StringComparison.OrdinalIgnoreCase)),
                BenchCount: result.Count(e => e.Status.Equals("Bench", StringComparison.OrdinalIgnoreCase))
            ));
        }

        private static EmployeeSummaryResponse MapToResponse(Employee employee, User user) => new(
            employee.Id,
            user.FullName,
            user.Email,
            user.Department,
            user.Designation,
            employee.Status.ToString(),
            user.IsActive
        );
    }
}
