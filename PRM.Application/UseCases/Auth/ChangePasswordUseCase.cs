using PRM.Application.DTOs.Auth;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Auth
{
    public class ChangePasswordUseCase(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        public async Task<Result> ExecuteAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return Result.Failure("Passwords do not match.");

            if (!IsStrongPassword(request.NewPassword))
                return Result.Failure("Password must be at least 8 characters with one uppercase letter and one number.");

            var user = await unitOfWork.Users.GetByIdAsync(userId, ct);
            if (user is null)
                return Result.Failure("User not found.");

            user.PasswordHash = passwordHasher.Hash(request.NewPassword);
            user.ForcePasswordChange = false;
            user.UpdatedAt = DateTime.UtcNow;

            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }

        // Private helper — validation logic belongs close to where it is used (Law of Demeter)
        private static bool IsStrongPassword(string password) =>
            password.Length >= 8 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsDigit);
    }
}
