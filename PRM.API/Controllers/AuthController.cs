using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PRM.Application.DTOs.Auth;
using PRM.Application.UseCases.Auth;
using LoginRequest = PRM.Application.DTOs.Auth.LoginRequest;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(LoginUseCase loginUseCase, ChangePasswordUseCase changePasswordUseCase) : ControllerBase
    {
        /// <summary>POST /api/auth/login</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await loginUseCase.ExecuteAsync(request, ct);

            if (!result.IsSuccess)
                return Unauthorized(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>POST /api/auth/change-password — requires valid JWT</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            // Extract the user ID from the JWT claims — not from the request body (security)
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            var result = await changePasswordUseCase.ExecuteAsync(userId, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Password changed successfully." });
        }
    }
}
