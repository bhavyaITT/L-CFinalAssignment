using PRM.Application.DTOs.Users;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Users
{
    public class ResetUserPasswordUseCase(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        public async Task<Result> ExecuteAsync(string username, ResetPasswordRequest request, CancellationToken ct = default)
        {
            if (!IsStrongPassword(request.NewTemporaryPassword))
                return Result.Failure("Temporary password must be 8+ characters with at least one uppercase letter and one number.");

            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
            if (user is null)
                return Result.Failure("User not found.");

            user.PasswordHash = passwordHasher.Hash(request.NewTemporaryPassword);
            user.ForcePasswordChange = true;  // Forces change on next login

            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }

        private static bool IsStrongPassword(string password) =>
            password.Length >= 8 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsDigit);
    }
}
