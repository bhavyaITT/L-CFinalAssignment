using PRM.Application.DTOs.Manager;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    public class GetResourceDashboardUseCase(IQueryService queryService)
    {
        public async Task<Result<ResourceDashboardResponse>> ExecuteAsync(CancellationToken ct = default)
        {
            var utilisations = (await queryService.GetActiveEmployeeUtilisationsAsync(ct)).ToList();

            var bench = utilisations
                .Where(e => e.TotalUtilisationPercent == 0)
                .OrderBy(e => e.FullName)
                .ToList();

            var active = utilisations
                .Where(e => e.TotalUtilisationPercent > 0)
                .OrderByDescending(e => e.TotalUtilisationPercent)
                .ToList();

            // For bench employees, load their top skills for display
            var benchResponses = new List<BenchEmployeeResponse>();
            foreach (var e in bench)
            {
                var detail = await queryService.GetEmployeeWithSkillsAsync(e.EmployeeId, ct);
                var skills = detail?.Skills.Select(s => s.SkillName).Take(5) ?? [];
                benchResponses.Add(new BenchEmployeeResponse(
                    e.EmployeeId, e.FullName, e.Department, e.Designation, skills
                ));
            }

            var activeResponses = active.Select(e =>
            {
                var free = 100 - e.TotalUtilisationPercent;
                var label = e.TotalUtilisationPercent >= 100
                    ? "FULL"
                    : $"{free}% free";

                return new ActiveEmployeeResponse(
                    e.EmployeeId, e.FullName, e.Department,
                    e.TotalUtilisationPercent, free, label
                );
            }).ToList();

            return Result<ResourceDashboardResponse>.Success(new ResourceDashboardResponse(
                BenchEmployees: benchResponses,
                ActiveEmployees: activeResponses,
                BenchCount: bench.Count,
                OverUtilisedCount: active.Count(e => e.TotalUtilisationPercent > 100),
                PartialCount: active.Count(e => e.TotalUtilisationPercent < 100)
            ));
        }
    }
}
