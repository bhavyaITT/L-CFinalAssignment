using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class Milestone : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
