using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class TimesheetEntry : BaseEntity
    {
        public int HoursWorked { get; set; }
        public string ActivityTags { get; set; } = string.Empty;

        public int TimesheetId { get; set; }
        public Timesheet Timesheet { get; set; } = null!;
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
