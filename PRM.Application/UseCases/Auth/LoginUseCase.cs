using PRM.Application;
using PRM.Application.DTOs.Auth;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;

namespace PRM.Application.UseCases.Auth
{
    public class LoginUseCase(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        public async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
        {
            var user = await unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
                return Result<LoginResponse>.Failure("Invalid username or password.");

            if (!user.IsActive)
                return Result<LoginResponse>.Failure("This account has been deactivated. Contact your administrator.");

            var token = tokenService.GenerateToken(user.Id, user.Username, user.Role.ToString());

            return Result<LoginResponse>.Success(new LoginResponse(
                UserId: user.Id,
                Username: user.Username,
                FullName: user.FullName,
                Role: user.Role.ToString(),
                Token: token,
                ForcePasswordChange: user.ForcePasswordChange
            ));
        }
    }
}
