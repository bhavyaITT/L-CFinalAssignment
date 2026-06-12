using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRM.Domain.Enums;
using PRM.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Job 1: Recomputes the Bench/Allocated status for every active employee.
    ///
    /// Why a scheduler job in addition to the immediate updates in allocation use cases?
    /// Edge cases: allocation ToDate expires naturally (no one manually ends it),
    /// or an allocation is created by the Admin directly without going through
    /// the Manager flow. The scheduler is the safety net — it ensures eventual consistency
    /// regardless of how data is modified.
    ///
    /// This demonstrates the Observer pattern: the job observes allocation data
    /// and updates employee status without being directly triggered by allocation changes.
    /// </summary>
    public class UtilisationRecomputeJob(PRMTDbContext context, ILogger<UtilisationRecomputeJob> logger)
    {
        public async Task ExecuteAsync()
        {
            logger.LogInformation("Starting utilisation recompute job at {Time}", DateTime.UtcNow);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var employees = await context.Employees
                .Where(e => e.User.IsActive)
                .Include(e => e.Allocations)
                .ToListAsync();

            int updatedCount = 0;

            foreach (var employee in employees)
            {
                var hasActiveAllocation = employee.Allocations
                    .Any(a => a.FromDate <= today && a.ToDate >= today);

                var correctStatus = hasActiveAllocation
                    ? EmployeeStatus.Allocated
                    : EmployeeStatus.Bench;

                if (employee.Status != correctStatus)
                {
                    employee.Status = correctStatus;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Utilisation recompute: updated {Count} employee statuses", updatedCount);
            }
            else
            {
                logger.LogInformation("Utilisation recompute: all statuses already correct");
            }
        }
    }
}
