using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class Timesheet : BaseEntity
    {
        public DateOnly WeekStartDate { get; set; }
        public int TotalHours { get; set; }
        public TimesheetStatus Status { get; set; } = TimesheetStatus.Submitted;

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public ICollection<TimesheetEntry> Entries { get; set; } = [];
    }
}
