namespace PRM.Client.UI;

public static class ActivityTags
{
    public static readonly string[] Options =
    [
        "Backend API Development",
        "Microservices / Architecture",
        "Database Design & Queries",
        "WebSocket / Real-time Features",
        "Frontend Development",
        "Code Review / Mentoring",
        "Bug Fixing",
        "DevOps / Deployment",
        "Testing & QA",
        "Documentation"
    ];

    public static void DisplayMenu()
    {
        for (var i = 0; i < Options.Length; i++)
            Console.WriteLine($"  {i + 1}.  {Options[i]}");
        Console.WriteLine($"  {Options.Length + 1}.  Other (type manually)");
    }

    public static string ResolveTags(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var resolved = new List<string>();

        foreach (var part in parts)
        {
            if (int.TryParse(part, out var index))
            {
                if (index >= 1 && index <= Options.Length)
                    resolved.Add(Options[index - 1]);
                else if (index == Options.Length + 1)
                {
                    var custom = ConsoleUi.ReadRequired("Enter custom tag: ");
                    resolved.Add(custom);
                }
            }
            else
            {
                resolved.Add(part);
            }
        }

        return string.Join(",", resolved);
    }
}
