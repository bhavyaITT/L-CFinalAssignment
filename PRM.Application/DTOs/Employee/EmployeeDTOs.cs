using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Employee
{
    public record CreateEmployeeRequest(
    int UserId,
    string FullName,
    string Email,
    string Department,
    string Designation
    );

    public record UpdateEmployeeRequest(
        string FullName,
        string Email,
        string Department,
        string Designation
    );

    public record AddSkillRequest(
        string SkillName,
        string Category,       // "Backend" | "Frontend" | "DevOps" | "QA" | "Other"
        string Proficiency     // "Beginner" | "Intermediate" | "Advanced"
    );

    public record UpdateSkillProficiencyRequest(
        string Proficiency
    );

    // ── Responses ─────────────────────────────────────────────────

    public record SkillResponse(
        int Id,
        string SkillName,
        string Category,
        string Proficiency
    );

    public record EmployeeSummaryResponse(
        int Id,
        string FullName,
        string Email,
        string Department,
        string Designation,
        string Status,
        bool IsActive
    );

    public record EmployeeDetailResponse(
        int Id,
        string FullName,
        string Email,
        string Department,
        string Designation,
        string Status,
        bool IsActive,
        IEnumerable<SkillResponse> Skills
    );

    public record EmployeesListResponse(
        IEnumerable<EmployeeSummaryResponse> Employees,
        int TotalCount,
        int AllocatedCount,
        int BenchCount
    );

}
