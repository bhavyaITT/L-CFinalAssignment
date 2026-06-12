using PRM.Application.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    /// <summary>
    /// Resolves the Employee record for the currently logged-in Manager.
    /// Every Manager endpoint needs this — the JWT contains UserId but business logic
    /// needs EmployeeId (managers are also employees with a Manager role).
    /// </summary>
    public class ResolveEmployeeIdUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<int>> ExecuteAsync(int userId, CancellationToken ct = default)
        {
            var employee = await unitOfWork.Employees.FirstOrDefaultAsync(e => e.Id == userId, ct);

            if (employee is null)
                return Result<int>.Failure("No employee profile found for this user account.");

            return Result<int>.Success(employee.Id);
        }
    }
}
