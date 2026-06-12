using PRM.Application.DTOs.Manager;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Manager
{
    /// <summary>
    /// Enforces all BRD allocation validation rules:
    /// 1. Total utilisation across overlapping allocations cannot exceed 100%
    /// 2. FromDate must be before ToDate
    /// 3. Project must be Active or Planned
    /// After saving, recomputes the employee's Bench/Allocated status.
    /// </summary>
    public class CreateAllocationUseCase(IUnitOfWork unitOfWork, IQueryService queryService)
    {
        public async Task<Result<CreateAllocationResponse>> ExecuteAsync(
            CreateAllocationRequest request,
            int requestingManagerEmployeeId,
            CancellationToken ct = default)
        {
            // ── Basic validation ──────────────────────────────────
            if (request.UtilisationPercentage < 1 || request.UtilisationPercentage > 100)
                return Result<CreateAllocationResponse>.Failure("Utilisation must be between 1 and 100.");

            if (request.FromDate >= request.ToDate)
                return Result<CreateAllocationResponse>.Failure("From date must be before To date.");

            // ── Employee must exist and be active ─────────────────
            var employee = await unitOfWork.Employees.GetByIdAsync(request.EmployeeId, ct);
            if (employee is null)
                return Result<CreateAllocationResponse>.Failure($"Employee with ID {request.EmployeeId} not found.");

            var employeeUser = await unitOfWork.Users.GetByIdAsync(request.EmployeeId, ct);
            if (employeeUser is null || !employeeUser.IsActive)
                return Result<CreateAllocationResponse>.Failure($"Active employee with ID {request.EmployeeId} not found.");

            // ── Project must be Active or Planned and belong to this manager ──
            var project = await unitOfWork.Projects.GetByIdAsync(request.ProjectId, ct);
            if (project is null)
                return Result<CreateAllocationResponse>.Failure($"Project with ID {request.ProjectId} not found.");

            if (project.ManagerId != requestingManagerEmployeeId)
                return Result<CreateAllocationResponse>.Failure("You can only allocate resources to your own projects.");

            if (project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.OnHold)
                return Result<CreateAllocationResponse>.Failure($"Cannot allocate to a project with status '{project.Status}'.");

            // ── Over-allocation guard (100% cap) ──────────────────
            var existingUtil = await queryService.GetEmployeeUtilisationInPeriodAsync(
                request.EmployeeId, request.FromDate, request.ToDate, null, ct);

            if (existingUtil + request.UtilisationPercentage > 100)
                return Result<CreateAllocationResponse>.Failure(
                    $"{employeeUser.FullName} is already at {existingUtil}% in this period. " +
                    $"Adding {request.UtilisationPercentage}% would exceed 100%.");

            // ── Save allocation ───────────────────────────────────
            var allocation = new Allocation
            {
                EmployeeId = request.EmployeeId,
                ProjectId = request.ProjectId,
                UtilisationPercentage = request.UtilisationPercentage,
                FromDate = request.FromDate,
                ToDate = request.ToDate
            };

            await unitOfWork.Allocations.AddAsync(allocation, ct);

            // ── Recompute employee status immediately ─────────────
            // If employee has any active allocation now → Allocated, else → Bench
            employee.Status = EmployeeStatus.Allocated;
            unitOfWork.Employees.Update(employee);

            await unitOfWork.SaveChangesAsync(ct);

            var newTotalUtil = existingUtil + request.UtilisationPercentage;

            return Result<CreateAllocationResponse>.Success(new CreateAllocationResponse(
                AllocationId: allocation.Id,
                EmployeeName: employeeUser.FullName,
                ProjectName: project.Name,
                UtilisationPercentage: allocation.UtilisationPercentage,
                FromDate: allocation.FromDate,
                ToDate: allocation.ToDate,
                NewTotalUtilisation: newTotalUtil
            ));
        }
    }
}
