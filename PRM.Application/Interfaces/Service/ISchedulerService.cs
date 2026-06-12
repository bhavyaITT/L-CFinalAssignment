using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.Interfaces.Service
{
    /// <summary>
    /// Defines the background scheduler contract.
    /// Application layer calls this to trigger jobs; Hangfire implements it.
    /// This is the Observer pattern: job execution is decoupled from the trigger.
    /// </summary>
    public interface ISchedulerService
    {
        void ScheduleRecurringJobs(int intervalHours);
        void TriggerUtilisationRecompute();
        void TriggerHealthFlagging();
        void TriggerMissedTimesheetDetection();
    }
}
