using Microsoft.Extensions.Configuration;
using PRM.Client.Flows;
using PRM.Client.Services;
using PRM.Client.UI;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7295";
var session = new UserSession();

var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
};
using var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

var api = new PrmApiClient(http, session);
var authFlow = new AuthFlow(api, session);
var adminFlow = new AdminFlow(api, session);
var managerFlow = new ManagerFlow(api, session);
var employeeFlow = new EmployeeFlow(api, session);

Console.OutputEncoding = System.Text.Encoding.UTF8;

while (true)
{
    if (!await authFlow.RunStartupAsync(CancellationToken.None))
    {
        ConsoleUi.ClearScreen();
        Console.WriteLine("Goodbye.");
        break;
    }

    switch (session.Role.ToUpperInvariant())
    {
        case "ADMIN":
            await adminFlow.RunAsync(CancellationToken.None);
            break;
        case "MANAGER":
            await managerFlow.RunAsync(CancellationToken.None);
            break;
        case "EMPLOYEE":
            await employeeFlow.RunAsync(CancellationToken.None);
            break;
        default:
            ConsoleUi.ShowError($"Unknown role: {session.Role}");
            session.Logout();
            break;
    }
}
