using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Manager
{
    public record CreateAllocationRequest(
     int EmployeeId,
     int ProjectId,
     int UtilisationPercentage,
     DateOnly FromDate,
     DateOnly ToDate
 );

    public record CreateAllocationResponse(
        int AllocationId,
        string EmployeeName,
        string ProjectName,
        int UtilisationPercentage,
        DateOnly FromDate,
        DateOnly ToDate,
        /// <summary>Recalculated total after this allocation is added.</summary>
        int NewTotalUtilisation
    );

    /// <summary>
    /// Returned before confirming — shows what the total will be so the
    /// frontend/console can display the "Validating..." line from the BRD.
    /// </summary>
    public record AllocationValidationResponse(
        int EmployeeId,
        string EmployeeName,
        int ExistingUtilisationInPeriod,
        int RequestedUtilisation,
        int TotalIfConfirmed,
        bool IsValid,
        string? ValidationMessage
    );

    public record EndAllocationResponse(
        int AllocationId,
        string EmployeeName,
        string ProjectName,
        DateOnly EndedOnDate,
        /// <summary>Status after ending — Bench if no other active allocations.</summary>
        string EmployeeNewStatus
    );

    /// <summary>Summary of one allocation for the End Allocation screen.</summary>
    public record AllocationOnProjectResponse(
        int AllocationId,
        int EmployeeId,
        string EmployeeName,
        int UtilisationPercent,
        DateOnly FromDate,
        DateOnly ToDate
    );
}
