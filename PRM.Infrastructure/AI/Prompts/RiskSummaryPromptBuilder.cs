using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Infrastructure.AI.Prompts
{
    /// <summary>
    /// Builds the Risk Summary prompt from structured project data.
    /// All facts come from the database — the LLM only explains, it does not invent.
    /// </summary>
    public static class RiskSummaryPromptBuilder
    {
        public static string BuildSystemPrompt() =>
            """
        You are a project health analyst for a software delivery company.
        Your job is to write a concise, plain-English risk summary for a project.
 
        You will receive factual data: milestone status, allocated resources,
        recent hours logged vs expected, and the project timeline.
 
        Write 3-5 sentences in a direct, professional tone.
        Focus on concrete risks: overdue milestones, low effort logging, approaching deadlines.
        Do not add generic advice. Do not repeat the raw numbers — interpret them.
        Return only the paragraph text, no headers, no bullet points.
        """;

        public static string BuildUserPrompt(RiskSummaryContext ctx)
        {
            var milestoneLines = ctx.Milestones.Select(m =>
            {
                var overdueNote = m.IsOverdue ? $" ⚠ OVERDUE by {m.DaysOverdue} days" : string.Empty;
                return $"  - {m.Title}: {m.Status} (Due {m.DueDate:dd-MMM-yyyy}){overdueNote}";
            });

            var resourceLines = ctx.Resources.Select(r =>
                $"  - {r.EmployeeName}: {r.AllocationPercent}% allocated, " +
                $"logged {r.HoursLastWeek} hrs last week (expected ~{r.ExpectedHoursPerWeek} hrs)");

            return $"""
            Project: {ctx.ProjectName}
            Status: {ctx.Status} | Health: {ctx.Health}
            Start: {ctx.StartDate:dd-MMM-yyyy} | End: {ctx.EndDate:dd-MMM-yyyy}
            Days until deadline: {ctx.DaysUntilDeadline}
 
            Milestones:
            {string.Join("\n", milestoneLines)}
 
            Allocated Resources (with last week's effort):
            {string.Join("\n", resourceLines)}
 
            Write the risk summary paragraph now.
            """;
        }
    }

    // ── Supporting context models ─────────────────────────────────

    public record RiskSummaryContext(
        string ProjectName,
        string Status,
        string Health,
        DateOnly StartDate,
        DateOnly EndDate,
        int DaysUntilDeadline,
        IEnumerable<MilestoneRiskItem> Milestones,
        IEnumerable<ResourceEffortItem> Resources
    );

    public record MilestoneRiskItem(
        string Title,
        string Status,
        DateOnly DueDate,
        bool IsOverdue,
        int DaysOverdue
    );

    public record ResourceEffortItem(
        string EmployeeName,
        int AllocationPercent,
        int HoursLastWeek,
        int ExpectedHoursPerWeek
    );

}
