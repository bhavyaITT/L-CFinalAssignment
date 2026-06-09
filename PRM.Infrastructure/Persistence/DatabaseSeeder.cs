using global::PRM.Domain.Entities;
using global::PRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PRM.Infrastructure.Persistence
{
    
    /// <summary>
    /// Seeds the first Admin account and default system configuration.
    /// Runs once at application startup. If the data already exists, it skips safely.
    /// Default Admin credentials: admin / Admin@1234 (must be changed on first login).
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(PRMTDbContext context)
        {
            await context.Database.MigrateAsync();

            await SeedAdminUserAsync(context);
            await SeedSystemConfigurationAsync(context);
        }

        private static async Task SeedAdminUserAsync(PRMTDbContext context)
        {
            if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
                return;

            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@intimetec.com",
                FullName = "System Administrator",
                // BCrypt hash of "Admin@1234"
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", 12),
                Role = UserRole.Admin,
                IsActive = true,
                ForcePasswordChange = true,  // Must change on first login
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            Console.WriteLine("✓ Admin seed account created. Login: admin / Admin@1234");
        }

        private static async Task SeedSystemConfigurationAsync(PRMTDbContext context)
        {
            if (await context.SystemConfigurations.AnyAsync())
                return;

            context.SystemConfigurations.Add(new SystemConfiguration
            {
                LlmProvider = "Gemini",
                LlmApiKey = string.Empty,
                SchedulerIntervalHours = 4,
                MaxWeeklyHours = 40,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            Console.WriteLine("✓ Default system configuration seeded.");
        }
    }
}
