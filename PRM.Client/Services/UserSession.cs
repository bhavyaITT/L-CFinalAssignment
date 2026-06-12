namespace PRM.Client.Services;

public sealed class UserSession
{
    public int UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public bool ForcePasswordChange { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public void SetLogin(int userId, string username, string fullName, string role, string token, bool forcePasswordChange)
    {
        UserId = userId;
        Username = username;
        FullName = fullName;
        Role = role;
        Token = token;
        ForcePasswordChange = forcePasswordChange;
    }

    public void ClearPasswordChangeFlag() => ForcePasswordChange = false;

    public void Logout()
    {
        UserId = 0;
        Username = string.Empty;
        FullName = string.Empty;
        Role = string.Empty;
        Token = string.Empty;
        ForcePasswordChange = false;
    }
}
