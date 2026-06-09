using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    public class UpdateEmployeeUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<EmployeeSummaryResponse>> ExecuteAsync(int employeeId, UpdateEmployeeRequest request, CancellationToken ct = default)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, ct);
            if (employee is null)
                return Result<EmployeeSummaryResponse>.Failure($"Employee with ID {employeeId} not found.");

            if (!employee.IsActive)
                return Result<EmployeeSummaryResponse>.Failure("Cannot update a deactivated employee.");

            employee.FullName = request.FullName;
            employee.Email = request.Email;
            employee.Department = request.Department;
            employee.Designation = request.Designation;

            unitOfWork.Employees.Update(employee);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<EmployeeSummaryResponse>.Success(new EmployeeSummaryResponse(
                employee.Id, employee.FullName, employee.Email,
                employee.Department, employee.Designation,
                employee.Status.ToString(), employee.IsActive
            ));
        }
    }

}
