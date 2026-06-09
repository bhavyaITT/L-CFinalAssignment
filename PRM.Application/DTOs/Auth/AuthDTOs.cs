using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Auth
{
    public record LoginRequest(string Username, string Password);

    public record LoginResponse(
        int UserId,
        string Username,
        string FullName,
        string Role,
        string Token,
        bool ForcePasswordChange
    );

    public record ChangePasswordRequest(string NewPassword, string ConfirmPassword);
}
