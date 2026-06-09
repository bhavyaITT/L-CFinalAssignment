using PRM.Application.DTOs;
using PRM.Application.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.SystemConfig
{
    public class GetSystemConfigUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<SystemConfigResponse>> ExecuteAsync(CancellationToken ct = default)
        {
            var config = (await unitOfWork.SystemConfigurations.GetAllAsync(ct)).FirstOrDefault();
            if (config is null)
                return Result<SystemConfigResponse>.Failure("System configuration not found. Please run the database seeder.");

            return Result<SystemConfigResponse>.Success(new SystemConfigResponse(
                Id: config.Id,
                LlmProvider: config.LlmProvider,
                HasApiKey: !string.IsNullOrEmpty(config.LlmApiKey),  // Never return the actual key
                SchedulerIntervalHours: config.SchedulerIntervalHours,
                MaxWeeklyHours: config.MaxWeeklyHours
            ));
        }
    }

    public class UpdateSystemConfigUseCase(IUnitOfWork unitOfWork)
    {
        private static readonly string[] AllowedProviders = ["Gemini", "Groq"];

        public async Task<Result<SystemConfigResponse>> ExecuteAsync(UpdateSystemConfigRequest request, CancellationToken ct = default)
        {
            var config = (await unitOfWork.SystemConfigurations.GetAllAsync(ct)).FirstOrDefault();
            if (config is null)
                return Result<SystemConfigResponse>.Failure("System configuration not found.");

            if (request.LlmProvider is not null)
            {
                if (!AllowedProviders.Contains(request.LlmProvider, StringComparer.OrdinalIgnoreCase))
                    return Result<SystemConfigResponse>.Failure($"Invalid LLM provider. Allowed: {string.Join(", ", AllowedProviders)}.");
                config.LlmProvider = request.LlmProvider;
            }

            if (request.LlmApiKey is not null)
                config.LlmApiKey = request.LlmApiKey;

            if (request.SchedulerIntervalHours.HasValue)
            {
                if (request.SchedulerIntervalHours.Value < 1 || request.SchedulerIntervalHours.Value > 24)
                    return Result<SystemConfigResponse>.Failure("Scheduler interval must be between 1 and 24 hours.");
                config.SchedulerIntervalHours = request.SchedulerIntervalHours.Value;
            }

            if (request.MaxWeeklyHours.HasValue)
            {
                if (request.MaxWeeklyHours.Value < 20 || request.MaxWeeklyHours.Value > 80)
                    return Result<SystemConfigResponse>.Failure("Max weekly hours must be between 20 and 80.");
                config.MaxWeeklyHours = request.MaxWeeklyHours.Value;
            }

            unitOfWork.SystemConfigurations.Update(config);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<SystemConfigResponse>.Success(new SystemConfigResponse(
                config.Id, config.LlmProvider,
                !string.IsNullOrEmpty(config.LlmApiKey),
                config.SchedulerIntervalHours, config.MaxWeeklyHours
            ));
        }
    }
}
