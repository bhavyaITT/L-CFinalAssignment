using PRM.Application.Interfaces.Service;
using PRM.Infrastructure.BackgroundJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;

namespace PRM.Infrastructure.ExternalService
{
    /// <summary>
    /// Implements ISchedulerService using Hangfire recurring jobs.
    /// Uses cron expressions for scheduling. Interval is configured
    /// from SystemConfiguration (set by Admin via System Config screen).
    ///
    /// Hangfire persists job state in SQL Server (same DB).
    /// Failed jobs are retried automatically with exponential backoff.
    /// The Hangfire dashboard at /hangfire shows job history and failures.
    /// </summary>
    public class HangfireSchedulerService(
        IRecurringJobManager recurringJobManager,
        IBackgroundJobClient backgroundJobClient) : ISchedulerService
    {
        // Stable job IDs — used to update/remove jobs without creating duplicates
        private const string UtilisationJobId = "prm-utilisation-recompute";
        private const string HealthJobId = "prm-project-health-flagging";
        private const string TimesheetJobId = "prm-missed-timesheet-detection";

        public void ScheduleRecurringJobs(int intervalHours)
        {
            // Convert intervalHours to a cron expression
            var cron = intervalHours switch
            {
                1 => Cron.Hourly(),
                24 => Cron.Daily(),
                _ => $"0 */{intervalHours} * * *"   // Every N hours
            };

            recurringJobManager.AddOrUpdate<UtilisationRecomputeJob>(
                UtilisationJobId,
                job => job.ExecuteAsync(),
                cron);

            recurringJobManager.AddOrUpdate<ProjectHealthFlaggingJob>(
                HealthJobId,
                job => job.ExecuteAsync(),
                cron);

            // Missed timesheet detection runs once per day at 9 AM UTC
            // (regardless of scheduler interval — it only needs to run daily)
            recurringJobManager.AddOrUpdate<MissedTimesheetDetectionJob>(
                TimesheetJobId,
                job => job.ExecuteAsync(),
                "0 9 * * *");
        }

        public void TriggerUtilisationRecompute() =>
            backgroundJobClient.Enqueue<UtilisationRecomputeJob>(job => job.ExecuteAsync());

        public void TriggerHealthFlagging() =>
            backgroundJobClient.Enqueue<ProjectHealthFlaggingJob>(job => job.ExecuteAsync());

        public void TriggerMissedTimesheetDetection() =>
            backgroundJobClient.Enqueue<MissedTimesheetDetectionJob>(job => job.ExecuteAsync());
    }
}
