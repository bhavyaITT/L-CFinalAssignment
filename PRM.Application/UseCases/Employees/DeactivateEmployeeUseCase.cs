using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    /// <summary>
    /// Deactivation rules per BRD:
    /// 1. All active allocations are ended immediately (ToDate = today)
    /// 2. The linked user account is blocked (IsActive = false)
    /// 3. All historical data (timesheets, past allocations) is preserved
    /// </summary>
    public class DeactivateEmployeeUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result> ExecuteAsync(int employeeId, CancellationToken ct = default)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, ct);
            if (employee is null)
                return Result.Failure($"Employee with ID {employeeId} not found.");

            if (!employee.IsActive)
                return Result.Failure("Employee is already deactivated.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // End all active allocations immediately
            var activeAllocations = (await unitOfWork.Allocations
                .FindAsync(a => a.EmployeeId == employeeId && a.ToDate >= today, ct))
                .ToList();

            foreach (var allocation in activeAllocations)
            {
                allocation.ToDate = today;
                unitOfWork.Allocations.Update(allocation);
            }

            // Deactivate employee profile
            employee.IsActive = false;

            // Block linked user account
            var user = await unitOfWork.Users.GetByIdAsync(employee.UserId, ct);
            if (user is not null)
            {
                user.IsActive = false;
                unitOfWork.Users.Update(user);
            }

            unitOfWork.Employees.Update(employee);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
