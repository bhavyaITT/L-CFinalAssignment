using PRM.Application.DTOs.Manager;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    public class GetEmployeeDrillInUseCase(IQueryService queryService)
    {
        private const int RecentWeeksCount = 4;

        public async Task<Result<EmployeeDrillInResponse>> ExecuteAsync(int employeeId, CancellationToken ct = default)
        {
            var detail = await queryService.GetEmployeeWithSkillsAsync(employeeId, ct);
            if (detail is null)
                return Result<EmployeeDrillInResponse>.Failure($"Employee with ID {employeeId} not found.");

            var allocations = (await queryService.GetActiveAllocationsForEmployeeAsync(employeeId, ct)).ToList();
            var totalUtil = allocations.Sum(a => a.UtilisationPercent);
            var recentTags = await queryService.GetRecentActivityTagsAsync(employeeId, RecentWeeksCount, ct);

            return Result<EmployeeDrillInResponse>.Success(new EmployeeDrillInResponse(
                Id: detail.Id,
                FullName: detail.FullName,
                Department: detail.Department,
                Designation: detail.Designation,
                Status: detail.Status,
                TotalUtilisationPercent: totalUtil,
                ProfileSkills: detail.Skills.Select(s => s.SkillName),
                ActiveAllocations: allocations,
                RecentActivityTags: recentTags
            ));
        }
    }
}
