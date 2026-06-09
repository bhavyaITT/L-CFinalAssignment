using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;
using PRM.Domain.Entities;
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
        public async Task<Result<EmployeesListResponse>> ExecuteAsync(
            string? statusFilter = null,
            string? departmentFilter = null,
            CancellationToken ct = default)
        {
            var all = (await unitOfWork.Employees.GetAllAsync(ct))
                .Where(e => e.IsActive)
                .ToList();

            var filtered = all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
                filtered = filtered.Where(e => string.Equals(e.Status.ToString(), statusFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(departmentFilter))
                filtered = filtered.Where(e => e.Department.Contains(departmentFilter, StringComparison.OrdinalIgnoreCase));

            var result = filtered.OrderBy(e => e.FullName).ToList();

            return Result<EmployeesListResponse>.Success(new EmployeesListResponse(
                Employees: result.Select(MapToResponse),
                TotalCount: result.Count,
                AllocatedCount: result.Count(e => e.Status.ToString() == "Allocated"),
                BenchCount: result.Count(e => e.Status.ToString() == "Bench")
            ));
        }

        private static EmployeeSummaryResponse MapToResponse(Employee e) => new(
            e.Id, e.FullName, e.Email, e.Department, e.Designation, e.Status.ToString(), e.IsActive
        );
    }
}
