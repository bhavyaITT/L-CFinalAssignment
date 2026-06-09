using PRM.Application.Interfaces.Repository;
using PRM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Users
{
    public class DeactivateUserUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result> ExecuteAsync(string username, CancellationToken ct = default)
        {
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
            if (user is null)
                return Result.Failure("User not found.");

            if (!user.IsActive)
                return Result.Failure("User is already inactive.");

            user.IsActive = false;

            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }

    public class ReactivateUserUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result> ExecuteAsync(string username, CancellationToken ct = default)
        {
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
            if (user is null)
                return Result.Failure("User not found.");

            if (user.IsActive)
                return Result.Failure("User is already active.");

            user.IsActive = true;

            int employeeId = (await unitOfWork.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id, ct))?.Id ?? 0;
            var employee = await unitOfWork.Employees.GetByIdAsync(employeeId, ct);
            if (employee is null)
                return Result.Failure("Linked employee profile not found.");
            employee.IsActive = true;

            unitOfWork.Users.Update(user);
            unitOfWork.Employees.Update(employee);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
