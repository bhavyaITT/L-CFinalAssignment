using PRM.Application.DTOs;
using PRM.Application.DTOs.Manager;
using PRM.Client.Services;
using PRM.Client.UI;

namespace PRM.Client.Flows;

public sealed class ManagerFlow(PrmApiClient api, UserSession session)
{
    public async Task RunAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteWelcomeHeader(session.FullName);
            Console.WriteLine();
            Console.WriteLine("1. Resource Dashboard");
            Console.WriteLine("2. Allocate Resource");
            Console.WriteLine("3. My Projects");
            Console.WriteLine("4. Timesheets");
            Console.WriteLine("5. AI Assistant");
            Console.WriteLine("6. Logout");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 6))
            {
                case 1: await ResourceDashboardAsync(ct); break;
                case 2: await AllocateResourceAsync(ct); break;
                case 3: await MyProjectsAsync(ct); break;
                case 4: await TeamTimesheetsAsync(ct); break;
                case 5: await AiAssistantAsync(ct); break;
                case 6: session.Logout(); return;
            }
        }
    }

    // ── Resource Dashboard ──────────────────────────────────────

    private async Task ResourceDashboardAsync(CancellationToken ct)
    {
        while (true)
        {
            var result = await api.GetResourceDashboardAsync(ct);
            if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
            var data = result.Data!;

            ConsoleUi.ClearScreen();
            var monthYear = DateTime.Now.ToString("MMMM yyyy");
            ConsoleUi.WriteHeader($"RESOURCE DASHBOARD — {monthYear}");
            Console.WriteLine();
            Console.WriteLine($"ON BENCH  ({data.BenchCount} employees available)");
            ConsoleUi.WriteDivider();
            Console.WriteLine($"{"ID",-5} {"Name",-18} {"Department",-12} {"Skills"}");

            foreach (var e in data.BenchEmployees)
                Console.WriteLine($"{e.Id,-5} {e.FullName,-18} {e.Department,-12} {string.Join(", ", e.Skills)}");

            Console.WriteLine();
            Console.WriteLine("ACTIVE EMPLOYEES");
            ConsoleUi.WriteDivider();
            Console.WriteLine($"{"ID",-5} {"Name",-18} {"Alloc %",-9} {"Availability"}");

            foreach (var e in data.ActiveEmployees)
                Console.WriteLine($"{e.Id,-5} {e.FullName,-18} {e.TotalUtilisationPercent,4}%  {e.AvailabilityLabel}");

            ConsoleUi.WriteDivider();
            Console.WriteLine($"Bench: {data.BenchCount}   |   Partial: {data.PartialCount}");
            Console.WriteLine("[D] Drill into employee details     [B] Back");

            var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
            if (action == "B") return;
            if (action == "D")
                await EmployeeDrillInAsync(ct);
        }
    }

    private async Task EmployeeDrillInAsync(CancellationToken ct)
    {
        var idText = ConsoleUi.ReadRequired("Enter Employee ID: ");
        if (!int.TryParse(idText, out var id)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        var result = await api.GetEmployeeDrillInAsync(id, ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
        var e = result.Data!;

        ConsoleUi.ClearScreen();
        Console.WriteLine($"── {e.FullName} ─────────────────────────────────");
        Console.WriteLine($"Department     : {e.Department}");
        Console.WriteLine($"Current Status : {e.Status.ToUpperInvariant()} ({e.TotalUtilisationPercent}%)");
        Console.WriteLine($"Profile Skills : {string.Join(", ", e.ProfileSkills)}");
        Console.WriteLine();
        Console.WriteLine("Active Allocations:");
        Console.WriteLine($"  {"Project",-16} {"%",-6} {"From",-12} {"To"}");
        foreach (var a in e.ActiveAllocations)
            Console.WriteLine($"  {a.ProjectName,-16} {a.UtilisationPercent,4}%  {ConsoleUi.FormatDate(a.FromDate),-12} {ConsoleUi.FormatDate(a.ToDate)}");

        Console.WriteLine();
        Console.WriteLine("Recent Activity Tags (last 4 weeks):");
        Console.WriteLine($"  {string.Join(", ", e.RecentActivityTags)}");
        Console.WriteLine();
        Console.WriteLine("[B] Back");
        ConsoleUi.ReadLine("");
    }

    // ── Allocate Resource ───────────────────────────────────────

    private async Task AllocateResourceAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("ALLOCATE RESOURCE");
            Console.WriteLine("1. Find resource using AI (recommended)");
            Console.WriteLine("2. Allocate directly (I already know who I want)");
            Console.WriteLine("3. End an existing allocation");
            Console.WriteLine("4. Back");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 4))
            {
                case 1: await AiAssistedAllocationAsync(ct); break;
                case 2: await DirectAllocationAsync(ct); break;
                case 3: await EndAllocationAsync(ct); break;
                case 4: return;
            }
        }
    }

    private async Task<int?> SelectProjectAsync(CancellationToken ct)
    {
        var projects = await api.GetMyProjectsAsync(ct);
        if (!projects.IsSuccess) { ConsoleUi.ShowApiError(projects.Error); return null; }
        var list = projects.Data!;
        if (list.Count == 0) { ConsoleUi.ShowError("No projects found."); ConsoleUi.Pause(); return null; }

        Console.WriteLine("Your projects:");
        for (var i = 0; i < list.Count; i++)
            Console.WriteLine($"  {list[i].Id}. {list[i].Name}");

        var input = ConsoleUi.ReadRequired("Enter project name or ID: ");
        var byId = list.FirstOrDefault(p => p.Id.ToString() == input);
        if (byId is not null) return byId.Id;

        var byName = list.FirstOrDefault(p => p.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
        return byName?.Id;
    }

    private async Task AiAssistedAllocationAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("ALLOCATE RESOURCE");
        Console.WriteLine("Step 1 — Select Project");
        var projectId = await SelectProjectAsync(ct);
        if (projectId is null) return;

        var projects = await api.GetMyProjectsAsync(ct);
        var projectName = projects.Data!.First(p => p.Id == projectId).Name;

        Console.WriteLine();
        Console.WriteLine("Step 2 — Describe your requirement");
        Console.WriteLine("Type what kind of resource you need:");
        var requirement = ConsoleUi.ReadRequired("> ");

        Console.WriteLine();
        Console.WriteLine("Searching... (AI matching in progress)");
        var match = await api.SkillMatchAsync(new SkillMatchRequest(requirement, projectName), ct);
        if (!match.IsSuccess) { ConsoleUi.ShowApiError(match.Error); return; }

        var candidates = match.Data!.Candidates.ToList();
        if (candidates.Count == 0)
        {
            ConsoleUi.ShowError("No matching employees found with sufficient availability.");
            ConsoleUi.Pause();
            return;
        }

        ConsoleUi.WriteDivider();
        Console.WriteLine("AI-MATCHED RESULTS");
        ConsoleUi.WriteDivider();
        Console.WriteLine($"{"#",-3} {"Name",-14} {"Free %",-8} {"Reason"}");
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var reason = c.AiReason.Length > 40 ? c.AiReason[..40] + "..." : c.AiReason;
            Console.WriteLine($"{i + 1,-3} {c.FullName,-14} {c.FreeCapacityPercent,4}%  {reason}");
        }

        Console.WriteLine();
        Console.WriteLine("Note: Suggestions are AI-generated. Verify before confirming.");
        ConsoleUi.WriteDivider();

        var choice = ConsoleUi.ReadMenuOption("Select employee (enter #, or 0 to cancel): ", 0, candidates.Count);
        if (choice == 0) return;

        var selected = candidates[choice - 1];
        await ConfirmAllocationAsync(selected.EmployeeId, projectId.Value, selected.SuggestedAllocationPercent, ct);
    }

    private async Task DirectAllocationAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("DIRECT ALLOCATION");
        var projectId = await SelectProjectAsync(ct);
        if (projectId is null) return;

        var empIdText = ConsoleUi.ReadRequired("Enter Employee ID : ");
        if (!int.TryParse(empIdText, out var employeeId)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        await ConfirmAllocationAsync(employeeId, projectId.Value, null, ct);
    }

    private async Task ConfirmAllocationAsync(int employeeId, int projectId, int? suggestedPercent, CancellationToken ct)
    {
        var drill = await api.GetEmployeeDrillInAsync(employeeId, ct);
        if (!drill.IsSuccess) { ConsoleUi.ShowApiError(drill.Error); return; }
        var emp = drill.Data!;

        Console.WriteLine();
        Console.WriteLine($"── {emp.FullName} ─────────────────────────────────");
        Console.WriteLine($"Current Utilisation: {emp.TotalUtilisationPercent}%");

        var defaultPct = suggestedPercent ?? 50;
        var pctText = ConsoleUi.ReadLine($"Utilisation % ({defaultPct}): ");
        var pct = int.TryParse(pctText, out var p) ? p : defaultPct;
        var fromDate = ConsoleUi.ReadDateRequired("From Date");
        var toDate = ConsoleUi.ReadDateRequired("To Date");

        var request = new CreateAllocationRequest(employeeId, projectId, pct, fromDate, toDate);

        Console.WriteLine();
        Console.WriteLine("Validating...");
        var validation = await api.ValidateAllocationAsync(request, ct);
        if (!validation.IsSuccess) { ConsoleUi.ShowApiError(validation.Error); return; }

        var v = validation.Data!;
        Console.WriteLine($"  {v.EmployeeName} total in this period: {v.ExistingUtilisationInPeriod}% + {v.RequestedUtilisation}% = {v.TotalIfConfirmed}%   {(v.IsValid ? "Valid" : "INVALID")}");
        if (!string.IsNullOrWhiteSpace(v.ValidationMessage))
            Console.WriteLine($"  {v.ValidationMessage}");

        if (!v.IsValid) { ConsoleUi.Pause(); return; }

        Console.WriteLine("[C] Confirm Allocation     [B] Back");
        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "C") return;

        var create = await api.CreateAllocationAsync(request, ct);
        if (!create.IsSuccess) ConsoleUi.ShowApiError(create.Error);
        else
        {
            var a = create.Data!;
            ConsoleUi.ShowSuccess($"Allocation saved. {a.EmployeeName} -> {a.ProjectName} ({a.UtilisationPercentage}%, {ConsoleUi.FormatDate(a.FromDate)}-{ConsoleUi.FormatDate(a.ToDate)})");
            ConsoleUi.Pause();
        }
    }

    private async Task EndAllocationAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("END ALLOCATION");
        var projectId = await SelectProjectAsync(ct);
        if (projectId is null) return;

        var result = await api.GetAllocationsOnProjectAsync(projectId.Value, ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
        var allocations = result.Data!;
        if (allocations.Count == 0) { ConsoleUi.ShowError("No active allocations on this project."); ConsoleUi.Pause(); return; }

        Console.WriteLine();
        Console.WriteLine("Active Allocations on this project:");
        Console.WriteLine($"  {"#",-3} {"Employee",-16} {"%",-6} {"From",-12} {"To"}");
        for (var i = 0; i < allocations.Count; i++)
        {
            var a = allocations[i];
            Console.WriteLine($"  {i + 1}.  {a.EmployeeName,-14} {a.UtilisationPercent,4}%  {ConsoleUi.FormatDate(a.FromDate),-12} {ConsoleUi.FormatDate(a.ToDate)}");
        }

        ConsoleUi.WriteDivider();
        var choice = ConsoleUi.ReadMenuOption("Select allocation to end: ", 1, allocations.Count);
        var selected = allocations[choice - 1];

        Console.WriteLine();
        Console.WriteLine($"End {selected.EmployeeName}'s allocation?");
        Console.WriteLine($"Set end date to today ({ConsoleUi.FormatDate(DateOnly.FromDateTime(DateTime.Today))})?");

        if (!ConsoleUi.Confirm("Confirm")) return;

        var end = await api.EndAllocationAsync(selected.AllocationId, ct);
        if (!end.IsSuccess) ConsoleUi.ShowApiError(end.Error);
        else
        {
            var e = end.Data!;
            ConsoleUi.ShowSuccess($"Allocation ended. {e.EmployeeName} freed from {e.ProjectName} as of {ConsoleUi.FormatDate(e.EndedOnDate)}.");
            Console.WriteLine($"Employee status updated to {e.EmployeeNewStatus.ToUpperInvariant()}.");
            ConsoleUi.Pause();
        }
    }

    // ── My Projects ─────────────────────────────────────────────

    private async Task MyProjectsAsync(CancellationToken ct)
    {
        var projects = await api.GetMyProjectsAsync(ct);
        if (!projects.IsSuccess) { ConsoleUi.ShowApiError(projects.Error); return; }
        var list = projects.Data!;

        if (list.Count == 0) { ConsoleUi.ShowError("No projects found."); ConsoleUi.Pause(); return; }

        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("MY PROJECTS");
        Console.WriteLine($"{"#",-4} {"Project",-18} {"End Date",-12} {"Health"}");
        ConsoleUi.WriteDivider();

        for (var i = 0; i < list.Count; i++)
            Console.WriteLine($"{i + 1}.  {list[i].Name,-16} {ConsoleUi.FormatDate(list[i].EndDate),-12} {ConsoleUi.HealthWithEmoji(list[i].Health)}");

        ConsoleUi.WriteDivider();
        var choice = ConsoleUi.ReadMenuOption("Select project number to view details: ", 1, list.Count);
        await ProjectDetailAsync(list[choice - 1].Id, ct);
    }

    private async Task ProjectDetailAsync(int projectId, CancellationToken ct)
    {
        while (true)
        {
            var result = await api.GetMyProjectDetailAsync(projectId, ct);
            if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
            var p = result.Data!;

            ConsoleUi.ClearScreen();
            Console.WriteLine($"── {p.Name} ───────────────────────────────");
            Console.WriteLine($"Health Status : {ConsoleUi.HealthWithEmoji(p.Health)}");
            Console.WriteLine();
            Console.WriteLine("Risk Flags:");
            foreach (var f in p.RiskFlags)
                Console.WriteLine($"  {(f.IsCritical ? "[X]" : "[OK]")}  {f.Message}");

            Console.WriteLine();
            Console.WriteLine("Milestones:");
            Console.WriteLine($"  {"#",-4} {"Title",-18} {"Due Date",-12} {"Status"}");
            var milestones = p.Milestones.ToList();
            for (var i = 0; i < milestones.Count; i++)
            {
                var m = milestones[i];
                var overdue = m.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) && m.DueDate < DateOnly.FromDateTime(DateTime.Today);
                Console.WriteLine($"  {i + 1}.  {m.Title,-16} {ConsoleUi.FormatDate(m.DueDate),-12} {m.Status}{(overdue ? "  OVERDUE" : "")}");
            }

            Console.WriteLine();
            Console.WriteLine("Allocated Resources:");
            Console.WriteLine($"  {"Name",-14} {"%",-6} {"From",-12} {"To"}");
            foreach (var a in p.AllocatedResources)
                Console.WriteLine($"  {a.EmployeeName,-14} {a.UtilisationPercent,4}%  {ConsoleUi.FormatDate(a.FromDate),-12} {ConsoleUi.FormatDate(a.ToDate)}");

            ConsoleUi.WriteDivider();
            Console.WriteLine("[A] Get AI Risk Summary     [B] Back");

            var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
            if (action == "B") return;
            if (action == "A")
                await ShowRiskSummaryAsync(projectId, ct);
        }
    }

    private async Task ShowRiskSummaryAsync(int projectId, CancellationToken ct)
    {
        Console.WriteLine();
        Console.WriteLine("Generating AI summary...");
        var result = await api.RiskSummaryAsync(projectId, ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

        ConsoleUi.WriteDivider();
        Console.WriteLine($"AI Risk Summary — {result.Data!.ProjectName}");
        ConsoleUi.WriteDivider();
        Console.WriteLine();
        Console.WriteLine($"\"{result.Data.Summary}\"");
        Console.WriteLine();
        Console.WriteLine($"  Note: {result.Data.AiNote}");
        ConsoleUi.Pause();
    }

    // ── Team Timesheets ─────────────────────────────────────────

    private async Task TeamTimesheetsAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("TIMESHEETS — MY TEAM");
            var weekInput = ConsoleUi.ReadLine("Filter by week (DD-MM-YYYY) or press Enter for current week: ");
            DateOnly? week = null;
            if (!string.IsNullOrWhiteSpace(weekInput) && ConsoleUi.TryParseDate(weekInput, out var parsed))
                week = parsed;

            var result = await api.GetTeamTimesheetsAsync(week, ct);
            if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
            var data = result.Data!;

            Console.WriteLine();
            Console.WriteLine($"Week: {ConsoleUi.FormatDate(data.WeekStartDate)}");
            ConsoleUi.WriteDivider();
            Console.WriteLine($"{"Employee",-16} {"Project",-16} {"Hrs",-6} {"Status"}");
            ConsoleUi.WriteDivider();

            foreach (var e in data.Entries)
            {
                var status = e.Status.Equals("Missed", StringComparison.OrdinalIgnoreCase) ? "MISSED !" : e.Status.ToUpperInvariant();
                Console.WriteLine($"{e.EmployeeName,-16} {e.ProjectName,-16} {e.HoursWorked,-6} {status}");
            }

            ConsoleUi.WriteDivider();
            Console.WriteLine("[V] View employee timesheet detail     [B] Back");

            var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
            if (action == "B") return;
            if (action == "V")
                await TeamTimesheetDetailAsync(data.WeekStartDate, ct);
        }
    }

    private async Task TeamTimesheetDetailAsync(DateOnly weekStart, CancellationToken ct)
    {
        var empIdText = ConsoleUi.ReadRequired("Enter Employee ID: ");
        if (!int.TryParse(empIdText, out var employeeId)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        var result = await api.GetTeamTimesheetDetailAsync(employeeId, weekStart, ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
        var d = result.Data!;

        ConsoleUi.ClearScreen();
        Console.WriteLine($"── {d.EmployeeName} — Week: {ConsoleUi.FormatDate(d.WeekStartDate)} — {d.Status.ToUpperInvariant()} ─────");
        Console.WriteLine($"{"Project",-16} {"Hrs",-6} {"Activity Tags"}");
        ConsoleUi.WriteDivider();

        foreach (var p in d.Projects)
            Console.WriteLine($"{p.ProjectName,-16} {p.HoursWorked,-6} {string.Join(", ", p.ActivityTags)}");

        Console.WriteLine($"Total: {d.TotalHours} hrs");
        ConsoleUi.Pause();
    }

    // ── AI Assistant ────────────────────────────────────────────

    private async Task AiAssistantAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("AI ASSISTANT");
            Console.WriteLine("1. Skill Match      — Find best employee for one requirement");
            Console.WriteLine("2. Team Staffing    — Staff all roles from bench in one search");
            Console.WriteLine("3. Risk Summary     — Get a health analysis for a project");
            Console.WriteLine("4. Back");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 4))
            {
                case 1: await AiSkillMatchAsync(ct); break;
                case 2: await AiTeamStaffingAsync(ct); break;
                case 3: await AiRiskSummaryMenuAsync(ct); break;
                case 4: return;
            }
        }
    }

    private async Task AiSkillMatchAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        Console.WriteLine("── Skill Match ────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("Describe your project requirement in plain English:");
        var requirement = ConsoleUi.ReadRequired("> ");

        Console.WriteLine();
        Console.WriteLine("Searching... (calling AI)");
        var result = await api.SkillMatchAsync(new SkillMatchRequest(requirement), ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

        var candidates = result.Data!.Candidates.ToList();
        Console.WriteLine();
        Console.WriteLine("Results:");
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            Console.WriteLine($"  {i + 1}.  {c.FullName}");
            Console.WriteLine($"      Reason: {c.AiReason}");
            if (c.SuggestedAllocationPercent.HasValue)
                Console.WriteLine($"      Suggested allocation: {c.SuggestedAllocationPercent}%");
            Console.WriteLine();
        }

        Console.WriteLine("  Note: These are AI-generated suggestions. Always verify availability");
        Console.WriteLine("  and skills with the employee before allocating.");
        Console.WriteLine();
        Console.WriteLine("[A] Go to Allocate Resource     [B] Back");

        var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
        if (action == "A")
            await AllocateResourceAsync(ct);
        else
            ConsoleUi.Pause();
    }

    private async Task AiTeamStaffingAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("TEAM STAFFING — BENCH ONLY");
        Console.WriteLine("Describe the team you need in plain English.");
        Console.WriteLine("Example: 5-person squad for React + .NET microservices with QA and a scrum master.");
        Console.WriteLine();
        var requirement = ConsoleUi.ReadRequired("> ");
        var projectContext = ConsoleUi.ReadLine("Project context (optional): ");

        Console.WriteLine();
        Console.WriteLine("Searching bench employees... (AI matching in progress)");
        var result = await api.TeamStaffingAsync(
            new TeamStaffingRequest(
                requirement,
                string.IsNullOrWhiteSpace(projectContext) ? null : projectContext),
            ct);

        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

        var data = result.Data!;
        ConsoleUi.WriteDivider();
        Console.WriteLine($"BENCH CANDIDATES REVIEWED: {data.BenchCandidatesConsidered}");
        Console.WriteLine();

        Console.WriteLine("SUGGESTED MATCHES");
        ConsoleUi.WriteDivider();
        foreach (var m in data.Matches)
        {
            Console.WriteLine($"  {m.RoleTitle}");
            Console.WriteLine($"    -> {m.EmployeeName} (ID {m.EmployeeId})");
            Console.WriteLine($"       {m.MatchReason}");
            Console.WriteLine();
        }

        if (data.Gaps.Any())
        {
            Console.WriteLine("GAPS");
            ConsoleUi.WriteDivider();
            foreach (var g in data.Gaps)
            {
                var gapLabel = g.GapType.Equals("AvailabilityGap", StringComparison.OrdinalIgnoreCase)
                    ? "AVAILABILITY GAP"
                    : "SKILL GAP";
                Console.WriteLine($"  {g.RoleTitle} — {gapLabel}");
                Console.WriteLine($"    {g.Reason}");
                if (!string.IsNullOrWhiteSpace(g.AllocatedEmployeeName))
                    Console.WriteLine($"    Allocated employee: {g.AllocatedEmployeeName}");
                if (g.NextAvailableDate.HasValue)
                    Console.WriteLine($"    Next available: {ConsoleUi.FormatDate(g.NextAvailableDate.Value)}");
                Console.WriteLine();
            }
        }

        Console.WriteLine($"Note: {data.AiNote}");
        ConsoleUi.Pause();
    }

    private async Task AiRiskSummaryMenuAsync(CancellationToken ct)
    {
        var projects = await api.GetMyProjectsAsync(ct);
        if (!projects.IsSuccess) { ConsoleUi.ShowApiError(projects.Error); return; }
        var list = projects.Data!;

        ConsoleUi.ClearScreen();
        Console.WriteLine("── Risk Summary ───────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("Select project:");
        for (var i = 0; i < list.Count; i++)
            Console.WriteLine($"  {i + 1}.  {list[i].Name}    {ConsoleUi.HealthWithEmoji(list[i].Health)}");

        var choice = ConsoleUi.ReadMenuOption("Enter project number: ", 1, list.Count);
        await ShowRiskSummaryAsync(list[choice - 1].Id, ct);
    }
}
