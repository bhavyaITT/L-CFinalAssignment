using System.Globalization;

namespace PRM.Client.UI;

public static class ConsoleUi
{
    public const int BoxWidth = 46;

    public static void ClearScreen() => Console.Clear();

    public static void WriteHeader(string title, string? subtitle = null)
    {
        WriteBoxTop();
        var innerWidth = BoxWidth - 2;
        var titleLine = title.Length > innerWidth ? title[..innerWidth] : title;
        WriteBoxLine(titleLine.PadRight(innerWidth));
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var sub = subtitle.Length > innerWidth ? subtitle[..innerWidth] : subtitle;
            WriteBoxLine(sub.PadRight(innerWidth));
        }
        WriteBoxBottom();
        Console.WriteLine();
    }

    public static void WriteWelcomeHeader(string name)
    {
        var now = DateTime.Now.ToString("dd-MMM-yyyy  HH:mm", CultureInfo.InvariantCulture);
        WriteHeader($"Welcome, {name}!", now);
    }

    public static void WriteDivider() => Console.WriteLine(new string('─', BoxWidth));

    public static string ReadLine(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static string ReadRequired(string prompt)
    {
        while (true)
        {
            var value = ReadLine(prompt);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            Console.WriteLine("This field is required.");
        }
    }

    public static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var password = string.Empty;
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write('*');
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }

    public static int ReadMenuOption(string prompt, int min, int max)
    {
        while (true)
        {
            var input = ReadLine(prompt);
            if (int.TryParse(input, out var option) && option >= min && option <= max)
                return option;

            Console.WriteLine($"Please enter a number between {min} and {max}.");
        }
    }

    public static bool Confirm(string message)
    {
        var input = ReadLine($"{message} [Y/N]: ").Trim().ToUpperInvariant();
        return input is "Y" or "YES";
    }

    public static void Pause(string message = "Press Enter to continue...")
    {
        Console.WriteLine();
        ReadLine(message);
    }

    public static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void ShowApiError(string? error)
    {
        ShowError(error ?? "An unexpected error occurred.");
        Pause();
    }

    public static DateOnly? ReadDateOptional(string prompt, string placeholder = "DD-MM-YYYY")
    {
        var input = ReadLine($"{prompt} ({placeholder}) or press Enter to skip: ");
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (TryParseDate(input, out var date))
            return date;

        ShowError($"Invalid date format. Use {placeholder}.");
        return ReadDateOptional(prompt, placeholder);
    }

    public static DateOnly ReadDateRequired(string prompt, string placeholder = "DD-MM-YYYY")
    {
        while (true)
        {
            var input = ReadRequired($"{prompt} ({placeholder}): ");
            if (TryParseDate(input, out var date))
                return date;

            ShowError($"Invalid date format. Use {placeholder}.");
        }
    }

    public static DateOnly? ReadDateOrDefault(string prompt, DateOnly defaultValue, string placeholder = "DD-MM-YYYY")
    {
        var input = ReadLine($"{prompt} ({placeholder}) or press Enter for {FormatDate(defaultValue)}: ");
        if (string.IsNullOrWhiteSpace(input))
            return defaultValue;

        if (TryParseDate(input, out var date))
            return date;

        ShowError($"Invalid date format. Use {placeholder}.");
        return ReadDateOrDefault(prompt, defaultValue, placeholder);
    }

    public static bool TryParseDate(string input, out DateOnly date)
    {
        var formats = new[] { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static string FormatDate(DateOnly date) =>
        date.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);

    public static DateOnly GetLastMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dayOfWeek = (int)today.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return today.AddDays(-daysFromMonday);
    }

    public static string FormatHealth(string health) => health.ToUpperInvariant() switch
    {
        "ONTRACK" or "ON_TRACK" => "ON TRACK",
        "ATTENTION" => "ATTENTION",
        "ATRISK" or "AT_RISK" => "AT RISK",
        _ => health.ToUpperInvariant()
    };

    public static string HealthIcon(string health) => health.ToUpperInvariant() switch
    {
        "ONTRACK" or "ON_TRACK" => "ON TRACK",
        "ATTENTION" => "ATTENTION",
        "ATRISK" or "AT_RISK" => "AT RISK",
        _ => health
    };

    public static string HealthDisplay(string health)
    {
        var normalized = health.Replace("_", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        return normalized switch
        {
            "ONTRACK" => "ON TRACK",
            "ATTENTION" => "ATTENTION",
            "ATRISK" => "AT RISK",
            _ => health
        };
    }

    public static string HealthEmoji(string health)
    {
        var normalized = health.Replace("_", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        return normalized switch
        {
            "ONTRACK" => "ON TRACK",
            "ATTENTION" => "ATTENTION",
            "ATRISK" => "AT RISK",
            _ => health
        };
    }

    public static string HealthWithEmoji(string health)
    {
        var normalized = health.Replace("_", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        return normalized switch
        {
            "ONTRACK" => "[OK] ON TRACK",
            "ATTENTION" => "[!!] ATTENTION",
            "ATRISK" => "[XX] AT RISK",
            _ => health
        };
    }

    private static void WriteBoxLine(string content)
    {
        var innerWidth = BoxWidth - 2;
        var line = content.Length > innerWidth ? content[..innerWidth] : content.PadRight(innerWidth);
        Console.WriteLine($"║ {line} ║");
    }

    private static void WriteBoxBottom() =>
        Console.WriteLine($"╚{new string('═', BoxWidth)}╝");

    public static void WriteBoxTop() =>
        Console.WriteLine($"╔{new string('═', BoxWidth)}╗");
}
