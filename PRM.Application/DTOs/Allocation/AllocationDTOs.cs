using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs.Allocation
{
    public record AllocationResponse(
     int Id,
     int EmployeeId,
     string EmployeeName,
     int ProjectId,
     string ProjectName,
     int UtilisationPercentage,
     DateOnly FromDate,
     DateOnly ToDate
 );

    public record AllocationsListResponse(
        IEnumerable<AllocationResponse> Allocations,
        int TotalActiveCount
    );

}
