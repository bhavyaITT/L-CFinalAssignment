using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class SystemConfiguration : BaseEntity
    {
        public string LlmProvider { get; set; } = "Gemini";

        /// <summary>Stored as plain text in dev. In production, use a secret manager.</summary>
        public string LlmApiKey { get; set; } = string.Empty;

        /// <summary>How often the background scheduler runs, in hours.</summary>
        public int SchedulerIntervalHours { get; set; } = 4;

        /// <summary>Max hours an employee can log per week across all projects.</summary>
        public int MaxWeeklyHours { get; set; } = 40;
    }
}
