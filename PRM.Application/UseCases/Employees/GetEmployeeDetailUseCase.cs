using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    public class GetEmployeeDetailUseCase(IQueryService queryService)
    {
        public async Task<Result<EmployeeDetailResponse>> ExecuteAsync(int employeeId, CancellationToken ct = default)
        {
            var employee = await queryService.GetEmployeeWithSkillsAsync(employeeId, ct);

            if (employee is null)
                return Result<EmployeeDetailResponse>.Failure($"Employee with ID {employeeId} not found.");

            return Result<EmployeeDetailResponse>.Success(employee);
        }
    }
}
