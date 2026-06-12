using PRM.Application.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    public class AssignManagerUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result> ExecuteAsync(int employeeId, int managerId, CancellationToken ct = default)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, ct);
            if (employee is null)
                return Result.Failure($"Employee with ID {employeeId} not found.");

            var manager = await unitOfWork.Employees.GetByIdAsync(managerId, ct);
            if (manager is null)
                return Result.Failure($"Manager with ID {managerId} not found.");

            employee.ManagerId = managerId;
            unitOfWork.Employees.Update(employee);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
