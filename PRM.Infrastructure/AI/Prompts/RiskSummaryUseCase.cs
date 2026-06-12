using Microsoft.EntityFrameworkCore;
using PRM.Application;
using PRM.Application.DTOs;
using PRM.Application.Interfaces.Service;
using PRM.Domain.Enums;
using PRM.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.AI.Prompts
{
    /// <summary>
    /// Risk Summary flow (per BRD):
    /// 1. Load project with milestones + allocated resources
    /// 2. For each resource, compute last week's logged hours vs expected hours
    /// 3. Build structured context object for the prompt
    /// 4. Call LLM and return the plain-English paragraph
    ///
    /// The LLM receives facts — it is not given raw SQL or DB access.
    /// It explains the data; it does not generate data.
    /// </summary>
    public class RiskSummaryUseCase(PRMTDbContext context, ILlmClientFactory llmFactory)
    {
        public async Task<Result<RiskSummaryResponse>> ExecuteAsync(
            int projectId, CancellationToken ct = default)
        {
            // ── Load project with full context ────────────────────
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var lastMonday = GetLastMonday();

            var project = await context.Projects
                .Include(p => p.Milestones)
                .Include(p => p.Allocations.Where(a => a.ToDate >= today))
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.User)
                .Include(p => p.Allocations.Where(a => a.ToDate >= today))
                    .ThenInclude(a => a.Employee)
                        .ThenInclude(e => e.Timesheets.Where(t => t.WeekStartDate == lastMonday))
                            .ThenInclude(t => t.Entries)
                .FirstOrDefaultAsync(p => p.Id == projectId, ct);

            if (project is null)
                return Result<RiskSummaryResponse>.Failure($"Project with ID {projectId} not found.");

            // Fetch system config for max weekly hours
            var config = await context.SystemConfigurations.FirstOrDefaultAsync(ct);
            var maxWeeklyHours = config?.MaxWeeklyHours ?? 40;

            // ── Build milestone risk items ─────────────────────────
            var milestoneItems = project.Milestones
                .OrderBy(m => m.DueDate)
                .Select(m =>
                {
                    var isOverdue = m.Status != MilestoneStatus.Done && m.DueDate < today;
                    var daysOverdue = isOverdue ? today.DayNumber - m.DueDate.DayNumber : 0;
                    return new MilestoneRiskItem(m.Title, m.Status.ToString(), m.DueDate, isOverdue, daysOverdue);
                })
                .ToList();

            // ── Build resource effort items ────────────────────────
            var resourceItems = project.Allocations.Select(a =>
            {
                var lastWeekEntry = a.Employee.Timesheets
                    .FirstOrDefault(t => t.WeekStartDate == lastMonday)
                    ?.Entries
                    .FirstOrDefault(e => e.ProjectId == projectId);

                var hoursLogged = lastWeekEntry?.HoursWorked ?? 0;

                // Expected = allocation% × maxWeeklyHours / 100
                var expectedHours = a.UtilisationPercentage * maxWeeklyHours / 100;

                return new ResourceEffortItem(
                    a.Employee.User.FullName,
                    a.UtilisationPercentage,
                    hoursLogged,
                    expectedHours
                );
            }).ToList();

            // ── Build context and call AI ─────────────────────────
            var riskCtx = new RiskSummaryContext(
                ProjectName: project.Name,
                Status: project.Status.ToString(),
                Health: project.Health.ToString(),
                StartDate: project.StartDate,
                EndDate: project.EndDate,
                DaysUntilDeadline: Math.Max(0, project.EndDate.DayNumber - today.DayNumber),
                Milestones: milestoneItems,
                Resources: resourceItems
            );

            ILlmClient llmClient;
            try
            {
                llmClient = await llmFactory.GetClientAsync(ct);
            }
            catch (InvalidOperationException ex)
            {
                return Result<RiskSummaryResponse>.Failure($"AI not available: {ex.Message}");
            }

            string summary;
            try
            {
                summary = await llmClient.CompleteAsync(
                    RiskSummaryPromptBuilder.BuildSystemPrompt(),
                    RiskSummaryPromptBuilder.BuildUserPrompt(riskCtx),
                    ct);
            }
            catch (Exception ex)
            {
                return Result<RiskSummaryResponse>.Failure($"AI call failed: {ex.Message}");
            }

            return Result<RiskSummaryResponse>.Success(new RiskSummaryResponse(
                ProjectId: project.Id,
                ProjectName: project.Name,
                Health: project.Health.ToString(),
                Summary: summary.Trim(),
                AiNote: "AI-generated from current milestone and timesheet data. Use alongside normal project management judgment."
            ));
        }

        private static DateOnly GetLastMonday()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dayOfWeek = (int)today.DayOfWeek;
            var daysToThisMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return today.AddDays(-daysToThisMonday - 7);
        }
    }
}
