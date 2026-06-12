using PRM.Application.DTOs.Manager;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    public class GetActiveAllocationsOnProjectUseCase(IUnitOfWork unitOfWork, IQueryService queryService)
    {
        public async Task<Result<IEnumerable<AllocationOnProjectResponse>>> ExecuteAsync(
            int projectId,
            int requestingManagerEmployeeId,
            CancellationToken ct = default)
        {
            var project = await unitOfWork.Projects.GetByIdAsync(projectId, ct);
            if (project is null)
                return Result<IEnumerable<AllocationOnProjectResponse>>.Failure($"Project with ID {projectId} not found.");

            if (project.ManagerId != requestingManagerEmployeeId)
                return Result<IEnumerable<AllocationOnProjectResponse>>.Failure("You can only view allocations for your own projects.");

            var allocations = await queryService.GetActiveAllocationsOnProjectAsync(projectId, ct);

            return Result<IEnumerable<AllocationOnProjectResponse>>.Success(allocations);
        }
    }

}
