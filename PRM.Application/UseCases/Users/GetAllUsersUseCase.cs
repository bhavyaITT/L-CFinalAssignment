using PRM.Application.DTOs.Users;
using PRM.Application.Interfaces.Repository;
using PRM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Users
{
    public class GetAllUsersUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<UsersListResponse>> ExecuteAsync(CancellationToken ct = default)
        {
            var users = (await unitOfWork.Users.GetAllAsync(ct))
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToList();

            var responses = users.Select(MapToResponse).ToList();

            return Result<UsersListResponse>.Success(new UsersListResponse(
                Users: responses,
                TotalCount: users.Count,
                ActiveCount: users.Count(u => u.IsActive),
                InactiveCount: users.Count(u => !u.IsActive)
            ));
        }

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
