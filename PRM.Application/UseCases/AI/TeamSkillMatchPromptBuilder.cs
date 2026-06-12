using PRM.Application.Interfaces.Service;

namespace PRM.Application.UseCases.AI;

public static class TeamSkillMatchPromptBuilder
{
    public static string BuildSystemPrompt() =>
        """
        You are a resource staffing assistant for a software delivery company.
        Your job is to read a manager's plain-English team requirement, infer the roles
        needed, and staff the team in ONE pass from BENCH employees only.

        You will receive:
        1. The manager's requirement (skills, team size, roles, duration, etc.)
        2. BENCH employees only — these are the ONLY people you may assign

        Steps:
        1. Infer the distinct roles the project needs (e.g. Backend Developer, QA Engineer, Scrum Master).
           Use sensible titles — do not invent more roles than the requirement implies.
        2. For each role, pick the best-matching bench employee or report a gap.

        Return ONLY valid JSON with this exact shape (no markdown, no extra text):
        {
          "matches": [
            {
              "roleTitle": "<inferred role title>",
              "employeeId": <integer>,
              "reason": "<why this bench employee fits>"
            }
          ],
          "gaps": [
            {
              "roleTitle": "<inferred role title>",
              "gapType": "SkillGap",
              "reason": "<clear explanation>",
              "nextAvailableDate": null,
              "allocatedEmployeeName": null
            }
          ]
        }

        Rules:
        - Assign at most ONE bench employee per role
        - NEVER assign the same employeeId to more than one role
        - Only use employeeId values from the BENCH list
        - If no suitable bench employee exists for a role, add a gaps entry instead of a match
        - gapType is always "SkillGap" (no bench employee has the required skills)
        - Every inferred role must appear exactly once in matches OR gaps
        - Do not invent employees, skills, or dates not present in the input
        """;

    public static string BuildUserPrompt(
        string requirementText,
        string? projectContext,
        IEnumerable<BenchEmployeeAiContext> benchEmployees)
    {
        var benchLines = benchEmployees.Select((e, i) =>
        {
            var skillText = e.Skills.Any()
                ? string.Join("; ", e.Skills.Select(s => $"{s.SkillName} ({s.Proficiency})"))
                : "No skills on profile";
            return $"""
            Bench {i + 1}:
              ID: {e.EmployeeId}
              Name: {e.FullName}
              Department: {e.Department} | Designation: {e.Designation}
              Skills: {skillText}
            """;
        });

        var projectLine = string.IsNullOrWhiteSpace(projectContext)
            ? string.Empty
            : $"Project context: {projectContext}\n\n";

        return $"""
        {projectLine}Team requirement:
        {requirementText.Trim()}

        BENCH employees (only assignable candidates):
        {string.Join("\n\n", benchLines)}

        Infer the roles needed, then return the JSON object now.
        """;
    }
}
