using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class Employee : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Bench;
        public bool IsActive { get; set; } = true;
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<EmployeeSkill> Skills { get; set; } = [];
        public ICollection<Allocation> Allocations { get; set; } = [];
        public ICollection<Timesheet> Timesheets { get; set; } = [];
    }
}
