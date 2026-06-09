using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Users;
using PRM.Application.UseCases.Users;
using PRM.Application.UseCases.Employees;
using ResetPasswordRequest = PRM.Application.DTOs.Users.ResetPasswordRequest;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]   // All user management is Admin-only
    public class UsersController(
        CreateUserUseCase createUser,
        GetAllUsersUseCase getAllUsers,
        ResetUserPasswordUseCase resetPassword,
        DeactivateUserUseCase deactivateUser,
        ReactivateUserUseCase reactivateUser) : ControllerBase
    {
        /// <summary>GET /api/users — List all users</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await getAllUsers.ExecuteAsync(ct);
            return Ok(result.Data);
        }

        /// <summary>POST /api/users — Create a new user account</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
        {
            var result = await createUser.ExecuteAsync(request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return CreatedAtAction(nameof(GetAll), new { id = result.Data!.Id }, result.Data);
        }

        /// <summary>PUT /api/users/{id}/reset-password — Reset a user's password</summary>
        [HttpPut("{username:string}/reset-password")]
        public async Task<IActionResult> ResetPassword(string username, [FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            var result = await resetPassword.ExecuteAsync(username, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Password reset. User will be prompted to change it on next login." });
        }

        /// <summary>PUT /api/users/{id}/deactivate — Block a user's login</summary>
        [HttpPut("{username: string}/deactivate")]
        public async Task<IActionResult> Deactivate(string username, CancellationToken ct)
        {
            var result = await deactivateUser.ExecuteAsync(username, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "User deactivated." });
        }

        /// <summary>PUT /api/users/{id}/reactivate — Re-enable a user's login</summary>
        [HttpPut("{username: string}/reactivate")]
        public async Task<IActionResult> Reactivate(string username, CancellationToken ct)
        {
            var result = await reactivateUser.ExecuteAsync(username, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Account reactivated. Previous allocations are NOT restored. Re-allocate manually if needed." });
        }
    }
}
