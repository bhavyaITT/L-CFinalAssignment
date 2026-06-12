using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PRM.API.Controllers;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using PRM.Application.UseCases;
using PRM.Application.UseCases.AI;
using PRM.Application.UseCases.Auth;
using PRM.Application.UseCases.Employees;
using PRM.Application.UseCases.Manager;
using PRM.Application.UseCases.Projects;
using PRM.Application.UseCases.SystemConfig;
using PRM.Application.UseCases.Users;
using PRM.Infrastructure.AI;
using PRM.Infrastructure.AI.Prompts;
using PRM.Infrastructure.BackgroundJobs;
using PRM.Infrastructure.ExternalService;
using PRM.Infrastructure.Persistence;
using PRM.Infrastructure.Repositories;
using System;
using System.Text;

namespace PRM.API
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PRMTDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly("PRM.Infrastructure")
                )
            );

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Infrastructure services
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IQueryService, EfQueryService>();

            // ── Auth ──────────────────────────────────────────────
            services.AddScoped<LoginUseCase>();
            services.AddScoped<ChangePasswordUseCase>();

            // ── Users ─────────────────────────────────────────────
            services.AddScoped<CreateUserUseCase>();
            services.AddScoped<GetAllUsersUseCase>();
            services.AddScoped<ResetUserPasswordUseCase>();
            services.AddScoped<DeactivateUserUseCase>();
            services.AddScoped<ReactivateUserUseCase>();

            // ── Employees ─────────────────────────────────────────
            services.AddScoped<AssignManagerUseCase>();
            services.AddScoped<CreateEmployeeUseCase>();
            services.AddScoped<GetAllEmployeesUseCase>();
            services.AddScoped<GetEmployeeDetailUseCase>();
            services.AddScoped<UpdateEmployeeUseCase>();
            services.AddScoped<DeactivateEmployeeUseCase>();

            // ── Skills ────────────────────────────────────────────
            services.AddScoped<GetSkillUseCase>();
            services.AddScoped<AddSkillUseCase>();
            services.AddScoped<UpdateSkillProficiencyUseCase>();
            services.AddScoped<RemoveSkillUseCase>();

            // ── Projects ──────────────────────────────────────────
            services.AddScoped<CreateProjectUseCase>();
            services.AddScoped<GetAllProjectsUseCase>();
            services.AddScoped<GetProjectDetailUseCase>();
            services.AddScoped<UpdateProjectUseCase>();

            // ── Milestones ────────────────────────────────────────
            services.AddScoped<AddMilestoneUseCase>();
            services.AddScoped<UpdateMilestoneStatusUseCase>();

            // ── Allocations ───────────────────────────────────────
            services.AddScoped<GetAllAllocationsUseCase>();

            // ── System Configuration ──────────────────────────────
            services.AddScoped<GetSystemConfigUseCase>();
            services.AddScoped<UpdateSystemConfigUseCase>();

            //// ── Manager Module (Phase 3) ──────────────────────────
            services.AddScoped<ResolveEmployeeIdUseCase>();
            services.AddScoped<GetResourceDashboardUseCase>();
            services.AddScoped<GetEmployeeDrillInUseCase>();
            services.AddScoped<ValidateAllocationUseCase>();
            services.AddScoped<CreateAllocationUseCase>();
            services.AddScoped<EndAllocationUseCase>();
            services.AddScoped<GetActiveAllocationsOnProjectUseCase>();
            services.AddScoped<GetMyProjectsUseCase>();
            services.AddScoped<GetMyProjectDetailUseCase>();
            services.AddScoped<GetTeamTimesheetsUseCase>();

            //// ── Employee Module (Phase 4) ──────────────────────────
            services.AddScoped<GetActiveAllocationsForWeekUseCase>();
            services.AddScoped<SubmitTimesheetUseCase>();
            services.AddScoped<GetMyTimesheetsUseCase>();
            services.AddScoped<GetMyAllocationsUseCase>();
            services.AddScoped<CheckMissedTimesheetUseCase>();

            // ── AI (Phase 5) ───────────────────────────────────────
            services.AddScoped<ILlmClientFactory, LlmClientFactory>();
            services.AddScoped<SkillMatchUseCase>();
            services.AddScoped<TeamSkillMatchUseCase>();
            services.AddScoped<RiskSummaryUseCase>();

            // ── Background Jobs (Phase 5) ──────────────────────────
            services.AddScoped<UtilisationRecomputeJob>();
            services.AddScoped<ProjectHealthFlaggingJob>();
            services.AddScoped<MissedTimesheetDetectionJob>();
            services.AddScoped<ISchedulerService, HangfireSchedulerService>();

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is missing from configuration.");

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    };
                });

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 2;
            });

            return services;
        }
    }
}
