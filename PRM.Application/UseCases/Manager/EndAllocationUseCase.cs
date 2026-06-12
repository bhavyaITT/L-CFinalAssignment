using PRM.Application.DTOs.Manager;
using PRM.Application.Interfaces.Repository;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    /// <summary>
    /// BRD rule: Only the manager who owns the project can end allocations on it.
    /// After ending, recomputes employee Bench/Allocated status immediately.
    /// The background scheduler will also recompute on its next cycle,
    /// but we update immediately so the Manager sees the correct status at once.
    /// </summary>
    public class EndAllocationUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<EndAllocationResponse>> ExecuteAsync(
            int allocationId,
            int requestingManagerEmployeeId,
            CancellationToken ct = default)
        {
            var allocation = await unitOfWork.Allocations.GetByIdAsync(allocationId, ct);
            if (allocation is null)
                return Result<EndAllocationResponse>.Failure($"Allocation with ID {allocationId} not found.");

            // Verify the manager owns the project this allocation belongs to
            var project = await unitOfWork.Projects.GetByIdAsync(allocation.ProjectId, ct);
            if (project is null || project.ManagerId != requestingManagerEmployeeId)
                return Result<EndAllocationResponse>.Failure("You can only end allocations on your own projects.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (allocation.ToDate < today)
                return Result<EndAllocationResponse>.Failure("This allocation has already ended.");

            allocation.ToDate = today;
            unitOfWork.Allocations.Update(allocation);

            // Recompute employee status — check if they have any remaining active allocations
            var employee = await unitOfWork.Employees.GetByIdAsync(allocation.EmployeeId, ct);
            if (employee is not null)
            {
                var hasOtherActiveAllocations = await unitOfWork.Allocations.AnyAsync(
                    a => a.EmployeeId == allocation.EmployeeId
                      && a.Id != allocationId
                      && a.ToDate >= today,
                    ct);

                employee.Status = hasOtherActiveAllocations
                    ? EmployeeStatus.Allocated
                    : EmployeeStatus.Bench;

                unitOfWork.Employees.Update(employee);
            }

            await unitOfWork.SaveChangesAsync(ct);

            var employeeUser = employee is not null
                ? await unitOfWork.Users.GetByIdAsync(employee.Id, ct)
                : null;

            return Result<EndAllocationResponse>.Success(new EndAllocationResponse(
                AllocationId: allocationId,
                EmployeeName: employeeUser?.FullName ?? "Unknown",
                ProjectName: project.Name,
                EndedOnDate: today,
                EmployeeNewStatus: employee?.Status.ToString() ?? "Unknown"
            ));
        }
    }
}
