using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class EmployeeSkill : BaseEntity
    {
        public string SkillName { get; set; } = string.Empty;
        public SkillCategory Category { get; set; }
        public ProficiencyLevel Proficiency { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
    }
}
