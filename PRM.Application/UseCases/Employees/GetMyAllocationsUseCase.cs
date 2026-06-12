using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Employees
{
    public class GetMyAllocationsUseCase(IQueryService queryService)
    {
        public async Task<Result<MyAllocationsResponse>> ExecuteAsync(
            int employeeId, CancellationToken ct = default)
        {
            var response = await queryService.GetMyAllocationsAsync(employeeId, ct);
            return Result<MyAllocationsResponse>.Success(response);
        }
    }

}
