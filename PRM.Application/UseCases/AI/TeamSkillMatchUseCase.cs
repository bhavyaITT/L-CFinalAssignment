using PRM.Application.DTOs;
using PRM.Application.Interfaces.Service;
using System.Text.Json;

namespace PRM.Application.UseCases.AI;

/// <summary>
/// Staffs a project team from a plain-English prompt using organisation-wide bench employees only.
/// Minimal DB reads: bench roster only — roles and skills are inferred by the LLM.
/// </summary>
public class TeamSkillMatchUseCase(IQueryService queryService, ILlmClientFactory llmFactory)
{
    public async Task<Result<TeamStaffingResponse>> ExecuteAsync(
        TeamStaffingRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RequirementText))
            return Result<TeamStaffingResponse>.Failure("Requirement text cannot be empty.");

        var benchEmployees = (await queryService.GetBenchEmployeesForTeamStaffingAsync(ct)).ToList();
        if (benchEmployees.Count == 0)
            return Result<TeamStaffingResponse>.Failure(
                "No employees are currently on bench organisation-wide. Cannot staff from bench.");

        ILlmClient llmClient;
        try
        {
            llmClient = await llmFactory.GetClientAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<TeamStaffingResponse>.Failure($"AI not available: {ex.Message}");
        }

        string rawResponse;
        try
        {
            rawResponse = await llmClient.CompleteAsync(
                TeamSkillMatchPromptBuilder.BuildSystemPrompt(),
                TeamSkillMatchPromptBuilder.BuildUserPrompt(
                    request.RequirementText, request.ProjectContext, benchEmployees),
                ct);
        }
        catch (Exception ex)
        {
            return Result<TeamStaffingResponse>.Failure($"AI call failed: {ex.Message}");
        }

        var parsed = ParseAiResponse(rawResponse);
        if (parsed is null)
            return Result<TeamStaffingResponse>.Failure(
                "AI returned an unreadable response. Please try again.");

        var benchById = benchEmployees.ToDictionary(e => e.EmployeeId);
        var usedEmployeeIds = new HashSet<int>();
        var matches = new List<TeamRoleMatchResponse>();
        var gaps = new List<TeamRoleGapResponse>();
        var coveredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in parsed.Matches)
        {
            if (string.IsNullOrWhiteSpace(match.RoleTitle))
                continue;

            coveredRoles.Add(match.RoleTitle);

            if (benchById.TryGetValue(match.EmployeeId, out var emp)
                && usedEmployeeIds.Add(match.EmployeeId))
            {
                matches.Add(new TeamRoleMatchResponse(
                    match.RoleTitle.Trim(), emp.EmployeeId, emp.FullName, match.Reason));
            }
            else
            {
                gaps.Add(new TeamRoleGapResponse(
                    match.RoleTitle.Trim(),
                    "SkillGap",
                    benchById.ContainsKey(match.EmployeeId)
                        ? "Employee already assigned to another role in this team."
                        : "AI suggested an invalid or non-bench employee for this role.",
                    null,
                    null));
            }
        }

        foreach (var gap in parsed.Gaps)
        {
            if (string.IsNullOrWhiteSpace(gap.RoleTitle)
                || !coveredRoles.Add(gap.RoleTitle))
                continue;

            gaps.Add(new TeamRoleGapResponse(
                gap.RoleTitle.Trim(),
                string.IsNullOrWhiteSpace(gap.GapType) ? "SkillGap" : gap.GapType.Trim(),
                gap.Reason,
                ParseDate(gap.NextAvailableDate),
                gap.AllocatedEmployeeName));
        }

        return Result<TeamStaffingResponse>.Success(new TeamStaffingResponse(
            request.RequirementText.Trim(),
            request.ProjectContext,
            matches,
            gaps,
            benchEmployees.Count,
            "Suggestions are AI-generated from bench employees only. Verify before allocating."));
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var d) ? d : null;

    private static AiTeamResult? ParseAiResponse(string raw)
    {
        try
        {
            var cleaned = raw
                .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("```", string.Empty)
                .Trim();

            var start = cleaned.IndexOf('{');
            var end = cleaned.LastIndexOf('}');
            if (start >= 0 && end > start)
                cleaned = cleaned[start..(end + 1)];

            return JsonSerializer.Deserialize<AiTeamResult>(
                cleaned,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private sealed class AiTeamResult
    {
        public List<AiMatch> Matches { get; set; } = [];
        public List<AiGap> Gaps { get; set; } = [];
    }

    private sealed class AiMatch
    {
        public string RoleTitle { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    private sealed class AiGap
    {
        public string RoleTitle { get; set; } = string.Empty;
        public string GapType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? NextAvailableDate { get; set; }
        public string? AllocatedEmployeeName { get; set; }
    }
}
