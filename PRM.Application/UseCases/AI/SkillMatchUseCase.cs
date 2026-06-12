using PRM.Application.DTOs;
using PRM.Application.Interfaces.Service;
using PRM.Infrastructure.AI.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.AI
{
    /// <summary>
    /// Skill Match flow (per BRD):
    /// 1. Load all active employees with skills, allocations, and recent activity tags
    /// 2. Filter out fully booked employees (100% utilisation) — they are excluded before any AI call
    /// 3. Parse the manager's requirement for an hours/week hint (part-time vs full-time)
    /// 4. Build structured prompt from remaining candidates
    /// 5. Call LLM and parse the ranked JSON response
    /// 6. Map AI results back to typed response with employee details
    /// </summary>
    public class SkillMatchUseCase(IQueryService queryService, ILlmClientFactory llmFactory)
    {
        private const int MinFreePercentForFullTime = 25;   // < 25% free → skip for full-time requests

        public async Task<Result<SkillMatchResponse>> ExecuteAsync(
            SkillMatchRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.RequirementText))
                return Result<SkillMatchResponse>.Failure("Requirement text cannot be empty.");

            // ── Step 1: Load all employees with AI context ────────
            var allEmployees = (await queryService.GetEmployeesForSkillMatchAsync(ct)).ToList();

            // ── Step 2: Filter based on availability ─────────────
            var partTimeHours = ParsePartTimeHours(request.RequirementText);
            var candidates = FilterCandidates(allEmployees, partTimeHours).ToList();

            if (!candidates.Any())
                return Result<SkillMatchResponse>.Failure(
                    "No employees currently have enough free capacity for this requirement. " +
                    "All active employees are fully booked.");

            // ── Step 3: Build prompt and call AI ──────────────────
            ILlmClient llmClient;
            try
            {
                llmClient = await llmFactory.GetClientAsync(ct);
            }
            catch (InvalidOperationException ex)
            {
                return Result<SkillMatchResponse>.Failure($"AI not available: {ex.Message}");
            }

            var systemPrompt = SkillMatchPromptBuilder.BuildSystemPrompt();
            var userPrompt = SkillMatchPromptBuilder.BuildUserPrompt(
                request.RequirementText, request.ProjectContext, candidates);

            string rawResponse;
            try
            {
                rawResponse = await llmClient.CompleteAsync(systemPrompt, userPrompt, ct);
            }
            catch (Exception ex)
            {
                return Result<SkillMatchResponse>.Failure($"AI call failed: {ex.Message}");
            }

            // ── Step 4: Parse AI response ─────────────────────────
            var aiResults = ParseAiResponse(rawResponse);
            if (aiResults is null)
                return Result<SkillMatchResponse>.Failure(
                    "AI returned an unreadable response. Please try again.");

            // ── Step 5: Map AI results back to full employee data ─
            var employeeById = candidates.ToDictionary(e => e.EmployeeId);
            var resultCandidates = aiResults
                .Where(r => employeeById.ContainsKey(r.EmployeeId))
                .Select(r =>
                {
                    var emp = employeeById[r.EmployeeId];
                    return new SkillMatchCandidateResponse(
                        emp.EmployeeId,
                        emp.FullName,
                        emp.Department,
                        emp.Designation,
                        emp.FreeCapacityPercent,
                        emp.ProfileSkills,
                        emp.RecentActivityTags,
                        AiReason: r.Reason,
                        SuggestedAllocationPercent: r.SuggestedAllocationPercent
                    );
                })
                .ToList();

            return Result<SkillMatchResponse>.Success(new SkillMatchResponse(
                RequirementText: request.RequirementText,
                Candidates: resultCandidates,
                TotalCandidatesEvaluated: candidates.Count,
                AiNote: "Results are AI-generated. Always verify availability and skills before confirming allocation."
            ));
        }

        // ── Private helpers ───────────────────────────────────────

        /// <summary>
        /// Parses phrases like "10 hours a week", "about 10 hrs/week" from the requirement text.
        /// Returns null if no specific hours are mentioned (full-time / open-ended).
        /// </summary>
        private static int? ParsePartTimeHours(string text)
        {
            // Simple regex: look for digits followed by "hour" or "hrs" near "week"
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"(\d+)\s*(hours?|hrs?)[\s/a-z]*week",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        private static IEnumerable<EmployeeAiContext> FilterCandidates(
            IEnumerable<EmployeeAiContext> employees, int? partTimeHoursPerWeek)
        {
            if (partTimeHoursPerWeek.HasValue)
            {
                // Part-time: need at least enough free hours for the requested commitment
                // Convert requested hours to approximate % (based on 40hr week)
                var neededPercent = (int)Math.Ceiling(partTimeHoursPerWeek.Value / 40.0 * 100);
                return employees.Where(e => e.FreeCapacityPercent >= neededPercent);
            }

            // Full-time / open-ended: exclude anyone with less than MinFreePercentForFullTime
            return employees.Where(e => e.FreeCapacityPercent >= MinFreePercentForFullTime);
        }

        private static List<AiRankResult>? ParseAiResponse(string raw)
        {
            try
            {
                var cleaned = raw
                    .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("```", string.Empty)
                    .Trim();

                var start = cleaned.IndexOf('[');
                var end = cleaned.LastIndexOf(']');
                if (start >= 0 && end > start)
                    cleaned = cleaned[start..(end + 1)];

                return JsonSerializer.Deserialize<List<AiRankResult>>(
                    cleaned,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        private record AiRankResult(
            int EmployeeId,
            string Reason,
            int? SuggestedAllocationPercent);
    }
}
