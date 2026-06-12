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
    /// <summary>
    /// Called before CreateAllocation to show the "Validating..." line from the BRD.
    /// The client calls this first, shows the validation result, then calls Create if confirmed.
    /// Separating validation from creation follows the Single Responsibility Principle.
    /// </summary>
    public class ValidateAllocationUseCase(IUnitOfWork unitOfWork, IQueryService queryService)
    {
        public async Task<Result<AllocationValidationResponse>> ExecuteAsync(
            CreateAllocationRequest request, CancellationToken ct = default)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(request.EmployeeId, ct);
            if (employee is null)
                return Result<AllocationValidationResponse>.Failure($"Employee with ID {request.EmployeeId} not found.");

            var employeeUser = await unitOfWork.Users.GetByIdAsync(request.EmployeeId, ct);
            if (employeeUser is null || !employeeUser.IsActive)
                return Result<AllocationValidationResponse>.Failure($"Active employee with ID {request.EmployeeId} not found.");

            var existing = await queryService.GetEmployeeUtilisationInPeriodAsync(
                request.EmployeeId, request.FromDate, request.ToDate, null, ct);

            var total = existing + request.UtilisationPercentage;
            var isValid = total <= 100;

            var message = isValid
                ? $"{employeeUser.FullName} total in this period: {existing}% + {request.UtilisationPercentage}% = {total}%   ✓ Valid"
                : $"{employeeUser.FullName} would be over-allocated: {existing}% + {request.UtilisationPercentage}% = {total}% (max 100%)";

            return Result<AllocationValidationResponse>.Success(new AllocationValidationResponse(
                EmployeeId: request.EmployeeId,
                EmployeeName: employeeUser.FullName,
                ExistingUtilisationInPeriod: existing,
                RequestedUtilisation: request.UtilisationPercentage,
                TotalIfConfirmed: total,
                IsValid: isValid,
                ValidationMessage: message
            ));
        }
    }
}
