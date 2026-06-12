using PRM.Application.DTOs.Manager;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    public class GetMyProjectsUseCase(IQueryService queryService)
    {
        public async Task<Result<IEnumerable<ManagerProjectSummaryResponse>>> ExecuteAsync(
            int managerEmployeeId, CancellationToken ct = default)
        {
            var projects = await queryService.GetManagerProjectsAsync(managerEmployeeId, ct);
            return Result<IEnumerable<ManagerProjectSummaryResponse>>.Success(projects);
        }
    }

    public class GetMyProjectDetailUseCase(IQueryService queryService)
    {
        public async Task<Result<ManagerProjectDetailResponse>> ExecuteAsync(
            int projectId, int managerEmployeeId, CancellationToken ct = default)
        {
            var detail = await queryService.GetManagerProjectDetailAsync(projectId, managerEmployeeId, ct);

            if (detail is null)
                return Result<ManagerProjectDetailResponse>.Failure(
                    "Project not found or you are not the manager of this project.");

            return Result<ManagerProjectDetailResponse>.Success(detail);
        }
    }
}
