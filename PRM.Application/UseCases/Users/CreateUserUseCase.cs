using PRM.Application.DTOs.Users;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Users
{
    public class CreateUserUseCase(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        public async Task<Result<UserSummaryResponse>> ExecuteAsync(int UserId, CreateUserRequest request, CancellationToken ct = default)
        {
            try
            {
                // Validate role string is a known value
                if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
                    return Result<UserSummaryResponse>.Failure($"Invalid role '{request.Role}'. Valid values: Admin, Manager, Employee.");

                // Enforce password strength — same rule as change-password
                if (!IsStrongPassword(request.TemporaryPassword))
                    return Result<UserSummaryResponse>.Failure("Temporary password must be 8+ characters with at least one uppercase letter and one number.");

                // Unique username check
                if (await unitOfWork.Users.AnyAsync(u => u.Username == request.Username, ct))
                    return Result<UserSummaryResponse>.Failure($"Username '{request.Username}' is already taken.");

                // Unique email check
                if (await unitOfWork.Users.AnyAsync(u => u.Email == request.Email, ct))
                    return Result<UserSummaryResponse>.Failure($"Email '{request.Email}' is already registered.");

                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Username = request.Username,
                    PasswordHash = passwordHasher.Hash(request.TemporaryPassword),
                    Role = role,
                    IsActive = true,
                    ForcePasswordChange = true,  // Always forced on Admin-created accounts
                    Department = request.Department,
                    Designation = request.Designation,
                    Employee = new Employee
                    {
                        Status = EmployeeStatus.Bench,
                        ManagerId = UserId
                    }
                };

                await unitOfWork.Users.AddAsync(user, ct);
                await unitOfWork.SaveChangesAsync(ct);
                //var employee = new Employee
                //{
                //    Id = user.Id,  // ✅ NOW VALID: Use the User's auto-generated Id
                //    Status = EmployeeStatus.Bench,
                //    ManagerId = UserId
                //};
                //await unitOfWork.Employees.AddAsync(employee, ct);
                //await unitOfWork.SaveChangesAsync(ct);
                return Result<UserSummaryResponse>.Success(MapToResponse(user));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception at create user case is " + ex.Message);
                return Result<UserSummaryResponse>.Failure("Unable to complete request");
            }
        }

        private static bool IsStrongPassword(string password) =>
            password.Length >= 8 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsDigit);

        private static UserSummaryResponse MapToResponse(User user) => new(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.IsActive
        );
    }
}
