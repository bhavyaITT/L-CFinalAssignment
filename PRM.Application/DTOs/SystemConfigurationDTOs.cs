using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.DTOs
{
    public record SystemConfigResponse(
     int Id,
     string LlmProvider,
     bool HasApiKey,           // Never return the actual key to the client
     int SchedulerIntervalHours,
     int MaxWeeklyHours
 );

    public record UpdateSystemConfigRequest(
        string? LlmProvider,
        string? LlmApiKey,
        int? SchedulerIntervalHours,
        int? MaxWeeklyHours
    );
}
