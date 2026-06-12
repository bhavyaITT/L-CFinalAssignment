using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Users
{
    public record CreateUserRequest(
     string FullName,
     string Email,
     string Username,
     string TemporaryPassword,
     string Role,         // "Admin" | "Manager" | "Employee"
     string Department,       // Required for Manager and Employee
     string Designation       // Required for Manager and Employee
 );

    public record ResetPasswordRequest(
        string NewTemporaryPassword
    );

    // ── Responses ─────────────────────────────────────────────────

    public record UserSummaryResponse(
        int Id,
        string Username,
        string FullName,
        string Email,
        string Role,
        bool IsActive
    );

    public record UsersListResponse(
        IEnumerable<UserSummaryResponse> Users,
        int TotalCount,
        int ActiveCount,
        int InactiveCount
    );

}
