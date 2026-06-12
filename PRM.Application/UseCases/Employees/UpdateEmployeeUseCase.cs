using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;

namespace PRM.Application.UseCases.Employees
{
    public class UpdateEmployeeUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<EmployeeSummaryResponse>> ExecuteAsync(int employeeId, UpdateEmployeeRequest request, CancellationToken ct = default)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, ct);
            if (employee is null)
                return Result<EmployeeSummaryResponse>.Failure($"Employee with ID {employeeId} not found.");

            var user = await unitOfWork.Users.GetByIdAsync(employeeId, ct);
            if (user is null)
                return Result<EmployeeSummaryResponse>.Failure($"User account for employee ID {employeeId} not found.");

            if (!user.IsActive)
                return Result<EmployeeSummaryResponse>.Failure("Cannot update a deactivated employee.");

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Department = request.Department;
            user.Designation = request.Designation;

            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<EmployeeSummaryResponse>.Success(new EmployeeSummaryResponse(
                employee.Id, user.FullName, user.Email,
                user.Department, user.Designation,
                employee.Status.ToString(), user.IsActive
            ));
        }
    }
}
