using PRM.Client.Services;
using PRM.Client.UI;

namespace PRM.Client.Flows;

public sealed class AuthFlow(PrmApiClient api, UserSession session)
{
    public async Task<bool> RunStartupAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("PROJECT & RESOURCE MANAGEMENT TOOL", "Learn & Code — Final Project");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Exit");
            Console.WriteLine();

            var choice = ConsoleUi.ReadMenuOption("Enter option: ", 1, 2);
            if (choice == 2)
                return false;

            if (!await LoginAsync(ct))
                continue;

            if (session.ForcePasswordChange)
            {
                if (!await ChangePasswordAsync(ct))
                {
                    session.Logout();
                    continue;
                }
            }

            return true;
        }
    }

    private async Task<bool> LoginAsync(CancellationToken ct)
    {
        Console.WriteLine();
        var username = ConsoleUi.ReadRequired("Username: ");
        var password = ConsoleUi.ReadPassword("Password  : ");

        var result = await api.LoginAsync(username, password, ct);
        if (!result.IsSuccess)
        {
            ConsoleUi.ShowApiError(result.Error);
            return false;
        }

        var data = result.Data!;
        session.SetLogin(data.UserId, data.Username, data.FullName, data.Role, data.Token, data.ForcePasswordChange);
        return true;
    }

    private async Task<bool> ChangePasswordAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("CHANGE PASSWORD", "You must set a new password to continue.");
            Console.WriteLine();

            var newPassword = ConsoleUi.ReadPassword("New Password        : ");
            var confirm = ConsoleUi.ReadPassword("Confirm Password    : ");
            ConsoleUi.WriteDivider();
            Console.WriteLine("[S] Save and Continue");
            Console.WriteLine();

            var action = ConsoleUi.ReadLine("Enter S to save: ").ToUpperInvariant();
            if (action != "S")
                continue;

            var result = await api.ChangePasswordAsync(newPassword, confirm, ct);
            if (!result.IsSuccess)
            {
                ConsoleUi.ShowApiError(result.Error);
                continue;
            }

            session.ClearPasswordChangeFlag();
            ConsoleUi.ShowSuccess("Password updated. Welcome!");
            ConsoleUi.Pause();
            return true;
        }
    }
}
