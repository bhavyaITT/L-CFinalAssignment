using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PRM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.Persistence
{
    public class PRMTDbContext: DbContext
    {
        public PRMTDbContext(DbContextOptions<PRMTDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Milestone> Milestones => Set<Milestone>();
        public DbSet<Allocation> Allocations => Set<Allocation>();
        public DbSet<Timesheet> Timesheets => Set<Timesheet>();
        public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
        public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
        //public DbSet<Role> Roles => Set<Role>();
        //public DbSet<UserRoleMapping> UserRoleMappings => Set<UserRoleMapping>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Applies all IEntityTypeConfiguration classes in this assembly automatically.
            // Adding a new config file is all that's needed — no changes here (Open/Closed Principle).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PRMTDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Auto-set UpdatedAt on every save — no need to remember in each use case
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified))
            {
                if (entry.Entity is PRM.Domain.Entities.BaseEntity entity)
                    entity.UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}