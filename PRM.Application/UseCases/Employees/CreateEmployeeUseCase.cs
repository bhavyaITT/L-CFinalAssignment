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
    public class CreateEmployeeUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<EmployeeSummaryResponse>> ExecuteAsync(CreateEmployeeRequest request, CancellationToken ct = default)
        {
            // The user must exist and must be a Manager or Employee (not Admin — Admins don't need employee profiles)
            var user = await unitOfWork.Users.GetByIdAsync(request.UserId, ct);
            if (user is null)
                return Result<EmployeeSummaryResponse>.Failure($"User with ID {request.UserId} not found.");

            if (user.Role == UserRole.Admin)
                return Result<EmployeeSummaryResponse>.Failure("Admin accounts do not need an employee profile.");

            // A user can only be linked to one employee profile
            if (await unitOfWork.Employees.AnyAsync(e => e.UserId == request.UserId, ct))
                return Result<EmployeeSummaryResponse>.Failure($"User ID {request.UserId} already has an employee profile.");

            var employee = new Employee
            {
                UserId = request.UserId,
                FullName = request.FullName,
                Email = request.Email,
                Department = request.Department,
                Designation = request.Designation,
                Status = EmployeeStatus.Bench,  // Always starts on Bench
                IsActive = true
            };

            await unitOfWork.Employees.AddAsync(employee, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<EmployeeSummaryResponse>.Success(MapToResponse(employee));
        }

        private static EmployeeSummaryResponse MapToResponse(Employee e) => new(
            e.Id, e.FullName, e.Email, e.Department, e.Designation, e.Status.ToString(), e.IsActive
        );
    }
}
