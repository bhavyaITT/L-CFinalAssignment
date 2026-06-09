using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Domain.Entities
{
    public class Allocation : BaseEntity
    {
        public int UtilisationPercentage { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
