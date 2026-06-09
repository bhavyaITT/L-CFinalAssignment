using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class Project : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
        public ProjectHealth Health { get; set; } = ProjectHealth.OnTrack;

        public int ManagerId { get; set; }
        public Employee Manager { get; set; } = null!;
        public ICollection<Milestone> Milestones { get; set; } = [];
        public ICollection<Allocation> Allocations { get; set; } = [];
    }
}
