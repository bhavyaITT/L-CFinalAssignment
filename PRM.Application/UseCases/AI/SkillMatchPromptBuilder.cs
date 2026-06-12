using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.AI.Prompts
{
    /// <summary>
    /// Factory responsible for building Skill Match prompts.
    /// Separating prompt construction from LLM invocation follows SRP:
    /// if the prompt format changes, only this class changes.
    /// The prompt uses structured JSON-like context so the LLM can
    /// reason over real employee data, not hallucinate.
    /// </summary>
    public static class SkillMatchPromptBuilder
    {
        public static string BuildSystemPrompt() =>
            """
        You are a resource planning assistant for a software delivery company.
        Your job is to recommend the best-fit employees for a project requirement.
 
        You will receive:
        1. A manager's requirement in plain English
        2. A list of available employees with their skills, free capacity, and recent work
 
        You must return ONLY a valid JSON array. No preamble, no explanation outside the JSON.
        Each element must have exactly these fields:
        {
          "employeeId": <integer>,
          "reason": "<one or two sentence plain-English explanation of why this person fits>",
          "suggestedAllocationPercent": <integer or null>
        }
 
        Rules:
        - Rank candidates best-fit first
        - Return at most 5 candidates
        - "reason" must reference actual skills or recent activity from the data provided
        - Set suggestedAllocationPercent only if the manager's requirement mentions specific hours (e.g. "10 hours a week")
        - Do not invent skills or availability not present in the data
        - If no candidates are suitable, return an empty array []
        """;

        public static string BuildUserPrompt(
            string requirementText,
            string? projectContext,
            IEnumerable<EmployeeAiContext> candidates)
        {
            var candidateLines = candidates.Select((e, i) =>
                $"""
            Candidate {i + 1}:
              ID: {e.EmployeeId}
              Name: {e.FullName}
              Department: {e.Department} | Designation: {e.Designation}
              Free Capacity: {e.FreeCapacityPercent}%
              Profile Skills: {(e.ProfileSkills.Any() ? string.Join(", ", e.ProfileSkills) : "None listed")}
              Recent Work Tags: {(e.RecentActivityTags.Any() ? string.Join(", ", e.RecentActivityTags) : "No recent timesheet data")}
            """);

            var projectLine = projectContext is not null
                ? $"Project Context: {projectContext}\n"
                : string.Empty;

            return $"""
            Manager Requirement:
            {requirementText}
 
            {projectLine}
            Available Candidates:
            {string.Join("\n\n", candidateLines)}
 
            Return only the JSON array as instructed.
            """;
        }
    }
}
