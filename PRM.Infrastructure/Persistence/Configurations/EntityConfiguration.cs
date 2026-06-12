using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");

            builder.Property(e => e.Id)
            .ValueGeneratedNever();
            //builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            //builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
            //builder.Property(e => e.Department).IsRequired().HasMaxLength(100);
            //builder.Property(e => e.Designation).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

            //builder.HasOne(e => e.User)
            //    .WithOne(u => u.Employee)
            //    .HasForeignKey<Employee>(e => e.UserId)
            //    .OnDelete(DeleteBehavior.Restrict);  // Never cascade-delete users when employees are deleted
            builder.HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.Id)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

            // Manager FK
            builder.HasOne(e => e.Manager)
                .WithMany(u => u.ManagedEmployees)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class EmployeeSkillConfiguration : IEntityTypeConfiguration<EmployeeSkill>
    {
        public void Configure(EntityTypeBuilder<EmployeeSkill> builder)
        {
            builder.ToTable("EmployeeSkills");

            builder.Property(s => s.SkillName).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(50);
            builder.Property(s => s.Proficiency).HasConversion<string>().HasMaxLength(50);

            builder.HasOne(s => s.Employee)
                .WithMany(e => e.Skills)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("Projects");

            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).HasMaxLength(1000);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(p => p.Health).HasConversion<string>().HasMaxLength(50);

            // Manager is an Employee — restrict delete so we can't accidentally delete a manager with projects
            builder.HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
    {
        public void Configure(EntityTypeBuilder<Milestone> builder)
        {
            builder.ToTable("Milestones");

            builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(50);

            builder.HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
    {
        public void Configure(EntityTypeBuilder<Allocation> builder)
        {
            builder.ToTable("Allocations");

            builder.HasOne(a => a.Employee)
                .WithMany(e => e.Allocations)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Project)
                .WithMany(p => p.Allocations)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
    {
        public void Configure(EntityTypeBuilder<Timesheet> builder)
        {
            builder.ToTable("Timesheets");

            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(50);

            // Composite unique index — one timesheet per employee per week
            builder.HasIndex(t => new { t.EmployeeId, t.WeekStartDate }).IsUnique();

            builder.HasOne(t => t.Employee)
                .WithMany(e => e.Timesheets)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
    {
        public void Configure(EntityTypeBuilder<TimesheetEntry> builder)
        {
            builder.ToTable("TimesheetEntries");

            builder.Property(te => te.ActivityTags).HasMaxLength(500);

            builder.HasOne(te => te.Timesheet)
                .WithMany(t => t.Entries)
                .HasForeignKey(te => te.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(te => te.Project)
                .WithMany()
                .HasForeignKey(te => te.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
    {
        public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
        {
            builder.ToTable("SystemConfigurations");

            builder.Property(s => s.LlmProvider).IsRequired().HasMaxLength(100);
            builder.Property(s => s.LlmApiKey).HasMaxLength(500);
        }
    }
}
