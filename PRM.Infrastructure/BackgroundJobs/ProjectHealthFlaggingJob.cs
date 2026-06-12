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
    /// Job 2: Recomputes ProjectHealth for all active projects.
    ///
    /// Health rules:
    /// - AtRisk    → any milestone is overdue AND not Done, OR any allocated resource
    ///               logged less than 50% of expected hours last week
    /// - Attention → any milestone due within 7 days is still NotStarted,
    ///               OR a resource logged less than 75% of expected hours last week
    /// - OnTrack   → none of the above
    ///
    /// Health is set on the Project entity. The Manager's "My Projects" screen
    /// reads this value — no computation at request time (pre-computed by the scheduler).
    /// This is the Separation of Concerns principle: computation is separated from display.
    /// </summary>
    public class ProjectHealthFlaggingJob(PRMTDbContext context, ILogger<ProjectHealthFlaggingJob> logger)
    {
        public async Task ExecuteAsync()
        {
            logger.LogInformation("Starting project health flagging job at {Time}", DateTime.UtcNow);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var lastMonday = GetLastMonday(today);
            var sevenDaysFromNow = today.AddDays(7);

            var config = await context.SystemConfigurations.FirstOrDefaultAsync();
            var maxWeeklyHours = config?.MaxWeeklyHours ?? 40;

            var projects = await context.Projects
                .Where(p => p.Status == ProjectStatus.Active)
                .Include(p => p.Milestones)
                .Include(p => p.Allocations.Where(a => a.FromDate <= today && a.ToDate >= today))
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.Timesheets.Where(t => t.WeekStartDate == lastMonday))
                            .ThenInclude(t => t.Entries)
                .ToListAsync();

            int updatedCount = 0;

            foreach (var project in projects)
            {
                var newHealth = ComputeHealth(project, today, sevenDaysFromNow, lastMonday, maxWeeklyHours);

                if (project.Health != newHealth)
                {
                    project.Health = newHealth;
                    updatedCount++;
                    logger.LogInformation("Project '{Name}' health changed to {Health}", project.Name, newHealth);
                }
            }

            if (updatedCount > 0)
                await context.SaveChangesAsync();

            logger.LogInformation("Health flagging complete: {Count} projects updated", updatedCount);
        }

        private static ProjectHealth ComputeHealth(
            Domain.Entities.Project project,
            DateOnly today,
            DateOnly sevenDaysFromNow,
            DateOnly lastMonday,
            int maxWeeklyHours)
        {
            var milestones = project.Milestones.ToList();

            // AtRisk check 1: any overdue milestone that isn't done
            var hasOverdueMilestone = milestones
                .Any(m => m.Status != MilestoneStatus.Done && m.DueDate < today);

            // AtRisk check 2: any resource logged less than 50% of expected hours last week
            var hasLowEffortAtRisk = project.Allocations.Any(a =>
            {
                var expected = a.UtilisationPercentage * maxWeeklyHours / 100;
                if (expected == 0) return false;

                var logged = a.Employee.Timesheets
                    .FirstOrDefault(t => t.WeekStartDate == lastMonday)
                    ?.Entries
                    .Where(e => e.ProjectId == project.Id)
                    .Sum(e => e.HoursWorked) ?? 0;

                return logged < expected * 0.5;
            });

            if (hasOverdueMilestone || hasLowEffortAtRisk)
                return ProjectHealth.AtRisk;

            // Attention check 1: a milestone is due within 7 days and not started
            var hasMilestoneAlmostDueNotStarted = milestones
                .Any(m => m.Status == MilestoneStatus.NotStarted
                       && m.DueDate >= today
                       && m.DueDate <= sevenDaysFromNow);

            // Attention check 2: a resource logged less than 75% of expected hours last week
            var hasLowEffortAttention = project.Allocations.Any(a =>
            {
                var expected = a.UtilisationPercentage * maxWeeklyHours / 100;
                if (expected == 0) return false;

                var logged = a.Employee.Timesheets
                    .FirstOrDefault(t => t.WeekStartDate == lastMonday)
                    ?.Entries
                    .Where(e => e.ProjectId == project.Id)
                    .Sum(e => e.HoursWorked) ?? 0;

                return logged < expected * 0.75;
            });

            if (hasMilestoneAlmostDueNotStarted || hasLowEffortAttention)
                return ProjectHealth.Attention;

            return ProjectHealth.OnTrack;
        }

        private static DateOnly GetLastMonday(DateOnly today)
        {
            var dayOfWeek = (int)today.DayOfWeek;
            var daysToThisMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return today.AddDays(-daysToThisMonday - 7);
        }
    }
}
