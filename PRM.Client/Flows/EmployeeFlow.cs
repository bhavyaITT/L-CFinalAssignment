using PRM.Application.DTOs.Employee;
using PRM.Client.Services;
using PRM.Client.UI;

namespace PRM.Client.Flows;

public sealed class EmployeeFlow(PrmApiClient api, UserSession session)
{
    public async Task RunAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteWelcomeHeader(session.FullName);

            var reminder = await api.GetTimesheetReminderAsync(ct);
            if (reminder.IsSuccess && reminder.Data!.IsMissing && reminder.Data.MissingWeekStart.HasValue)
            {
                Console.WriteLine();
                Console.WriteLine($"  !  Reminder: Timesheet for week {ConsoleUi.FormatDate(reminder.Data.MissingWeekStart.Value)} has not been submitted.");
            }

            Console.WriteLine();
            ConsoleUi.WriteDivider();
            Console.WriteLine("1. Submit Timesheet");
            Console.WriteLine("2. View My Timesheets");
            Console.WriteLine("3. View My Allocations");
            Console.WriteLine("4. Logout");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 4))
            {
                case 1: await SubmitTimesheetAsync(ct); break;
                case 2: await ViewMyTimesheetsAsync(ct); break;
                case 3: await ViewMyAllocationsAsync(ct); break;
                case 4: session.Logout(); return;
            }
        }
    }

    private async Task SubmitTimesheetAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("SUBMIT TIMESHEET");
        Console.WriteLine($"Employee  : {session.FullName}");
        Console.WriteLine("Week Start: Enter date (DD-MM-YYYY) or press Enter for last Monday");

        var defaultWeek = ConsoleUi.GetLastMonday();
        var week = ConsoleUi.ReadDateOrDefault("", defaultWeek) ?? defaultWeek;

        Console.WriteLine();
        Console.WriteLine("Checking your active allocations for this week...");

        var allocResult = await api.GetAllocationsForWeekAsync(week, ct);
        if (!allocResult.IsSuccess) { ConsoleUi.ShowApiError(allocResult.Error); return; }
        var allocData = allocResult.Data!;
        var allocations = allocData.Allocations.ToList();

        if (allocations.Count == 0)
        {
            ConsoleUi.ShowError("No active allocations found for this week.");
            ConsoleUi.Pause();
            return;
        }

        var entries = new List<TimesheetEntryRequest>();
        var totalHours = 0;

        for (var i = 0; i < allocations.Count; i++)
        {
            var a = allocations[i];
            ConsoleUi.WriteDivider();
            Console.WriteLine($"PROJECT {i + 1} OF {allocations.Count} — {a.ProjectName}");
            Console.WriteLine($"  Allocation: {a.UtilisationPercentage}%   |   Expected: {a.MaxHoursForProject} hrs max");
            ConsoleUi.WriteDivider();

            var hoursText = ConsoleUi.ReadRequired("Hours worked this week: ");
            if (!int.TryParse(hoursText, out var hours) || hours < 0)
            {
                ConsoleUi.ShowError("Invalid hours.");
                ConsoleUi.Pause();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("What did you work on? Select activity tags:");
            ActivityTags.DisplayMenu();
            Console.WriteLine();
            var tagInput = ConsoleUi.ReadLine("Select tags (comma-separated): ");
            var tags = ActivityTags.ResolveTags(tagInput);

            entries.Add(new TimesheetEntryRequest(a.ProjectId, hours, tags));
            totalHours += hours;
        }

        ConsoleUi.WriteDivider();
        Console.WriteLine("SUMMARY");
        for (var i = 0; i < allocations.Count; i++)
            Console.WriteLine($"  {allocations[i].ProjectName,-16} {entries[i].HoursWorked} hrs    [{entries[i].ActivityTags}]");

        Console.WriteLine($"  {new string('─', 40)}");
        var valid = totalHours <= allocData.MaxWeeklyHours;
        Console.WriteLine($"  Total           {totalHours} hrs / {allocData.MaxWeeklyHours} hrs max   {(valid ? "OK" : "EXCEEDS LIMIT")}");
        ConsoleUi.WriteDivider();
        Console.WriteLine("[S] Submit Timesheet     [B] Back");

        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "S") return;

        var request = new SubmitTimesheetRequest(week, entries);
        var result = await api.SubmitTimesheetAsync(request, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else
        {
            ConsoleUi.ShowSuccess($"Timesheet submitted successfully. Status: {result.Data!.Status.ToUpperInvariant()}");
            ConsoleUi.Pause();
        }
    }

    private async Task ViewMyTimesheetsAsync(CancellationToken ct)
    {
        while (true)
        {
            var result = await api.GetMyTimesheetsAsync(ct);
            if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("MY TIMESHEETS");
            Console.WriteLine($"{"Week Start",-14} {"Total Hrs",-12} {"Status"}");
            ConsoleUi.WriteDivider();

            var timesheets = result.Data!.Timesheets.ToList();
            foreach (var t in timesheets)
            {
                var status = t.Status.Equals("Missed", StringComparison.OrdinalIgnoreCase) ? "MISSED !" : t.Status.ToUpperInvariant();
                Console.WriteLine($"{ConsoleUi.FormatDate(t.WeekStartDate),-14} {t.TotalHours,3} hrs   {status}");
            }

            ConsoleUi.WriteDivider();
            Console.WriteLine("[V] View week details     [B] Back");

            var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
            if (action == "B") return;
            if (action == "V")
                await ViewWeekDetailAsync(ct);
        }
    }

    private async Task ViewWeekDetailAsync(CancellationToken ct)
    {
        var week = ConsoleUi.ReadDateRequired("Enter week start date");
        var result = await api.GetMyTimesheetDetailAsync(week, ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
        var d = result.Data!;

        ConsoleUi.ClearScreen();
        Console.WriteLine($"── Week: {ConsoleUi.FormatDate(d.WeekStartDate)} — Status: {d.Status.ToUpperInvariant()} ─────");
        Console.WriteLine($"{"Project",-16} {"Hrs",-6} {"Activity Tags"}");
        ConsoleUi.WriteDivider();

        foreach (var e in d.Entries)
            Console.WriteLine($"{e.ProjectName,-16} {e.HoursWorked,-6} {string.Join(", ", e.ActivityTags)}");

        Console.WriteLine($"Total: {d.TotalHours} hrs");
        ConsoleUi.Pause();
    }

    private async Task ViewMyAllocationsAsync(CancellationToken ct)
    {
        var result = await api.GetMyAllocationsAsync(ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("MY ALLOCATIONS");
        Console.WriteLine($"{"Project",-18} {"%",-6} {"From",-12} {"To",-12} {"Status"}");
        ConsoleUi.WriteDivider();

        foreach (var a in result.Data!.Allocations)
            Console.WriteLine($"{a.ProjectName,-18} {a.UtilisationPercentage,4}%  {ConsoleUi.FormatDate(a.FromDate),-12} {ConsoleUi.FormatDate(a.ToDate),-12} {a.AllocationStatus.ToUpperInvariant()}");

        ConsoleUi.WriteDivider();
        Console.WriteLine($"Total Utilisation: {result.Data.TotalUtilisationPercent}%");
        Console.WriteLine("[B] Back");
        ConsoleUi.ReadLine("");
    }
}
