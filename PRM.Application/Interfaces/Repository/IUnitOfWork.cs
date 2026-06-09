using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Repository
{
    /// <summary>
    /// Unit of Work pattern.
    /// Ensures multiple repository operations commit or rollback together.
    /// One SaveChangesAsync call per business operation — no partial saves.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Employee> Employees { get; }
        IRepository<EmployeeSkill> EmployeeSkills { get; }
        IRepository<Project> Projects { get; }
        IRepository<Milestone> Milestones { get; }
        IRepository<Allocation> Allocations { get; }
        IRepository<Timesheet> Timesheets { get; }
        IRepository<TimesheetEntry> TimesheetEntries { get; }
        IRepository<SystemConfiguration> SystemConfigurations { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
