namespace Limp.Client.Services.UserIdentityService;

public static class ActiveUserIdentity
{
    private static string Username = string.Empty;

    public static void SetUsername(string username)
    {
        if (!string.IsNullOrWhiteSpace(username))
            Username = username;
    }

    public static string GetUsername()
    {
        return Username;
    }
}