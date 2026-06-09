using PRM.Application.Interfaces.Repository;
using PRM.Domain.Entities;
using PRM.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.Repositories
{
    /// <summary>
    /// Implements UnitOfWork by sharing a single AppDbContext across all repositories.
    /// All repositories in one request see the same EF Core change tracker.
    /// One SaveChangesAsync call commits everything atomically.
    /// </summary>
    public class UnitOfWork(PRMTDbContext context) : IUnitOfWork
    {
        // Lazy initialisation — repositories are created only when first accessed
        private IRepository<User>? _users;
        private IRepository<Employee>? _employees;
        private IRepository<EmployeeSkill>? _employeeSkills;
        private IRepository<Project>? _projects;
        private IRepository<Milestone>? _milestones;
        private IRepository<Allocation>? _allocations;
        private IRepository<Timesheet>? _timesheets;
        private IRepository<TimesheetEntry>? _timesheetEntries;
        private IRepository<SystemConfiguration>? _systemConfigurations;

        public IRepository<User> Users => _users ??= new Repository<User>(context);
        public IRepository<Employee> Employees => _employees ??= new Repository<Employee>(context);
        public IRepository<EmployeeSkill> EmployeeSkills => _employeeSkills ??= new Repository<EmployeeSkill>(context);
        public IRepository<Project> Projects => _projects ??= new Repository<Project>(context);
        public IRepository<Milestone> Milestones => _milestones ??= new Repository<Milestone>(context);
        public IRepository<Allocation> Allocations => _allocations ??= new Repository<Allocation>(context);
        public IRepository<Timesheet> Timesheets => _timesheets ??= new Repository<Timesheet>(context);
        public IRepository<TimesheetEntry> TimesheetEntries => _timesheetEntries ??= new Repository<TimesheetEntry>(context);
        public IRepository<SystemConfiguration> SystemConfigurations => _systemConfigurations ??= new Repository<SystemConfiguration>(context);

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            context.SaveChangesAsync(ct);

        public void Dispose() => context.Dispose();
    }
}
