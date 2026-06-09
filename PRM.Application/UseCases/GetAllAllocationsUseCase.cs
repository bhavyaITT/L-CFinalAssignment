using PRM.Application.DTOs.Allocation;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases
{
    public class GetAllAllocationsUseCase(IQueryService queryService)
    {
        public async Task<Result<AllocationsListResponse>> ExecuteAsync(
            int? employeeId = null,
            int? projectId = null,
            bool activeOnly = true,
            CancellationToken ct = default)
        {
            var allocations = (await queryService.GetAllocationsAsync(employeeId, projectId, activeOnly, ct)).ToList();

            return Result<AllocationsListResponse>.Success(new AllocationsListResponse(
                Allocations: allocations,
                TotalActiveCount: allocations.Count
            ));
        }
    }
}
