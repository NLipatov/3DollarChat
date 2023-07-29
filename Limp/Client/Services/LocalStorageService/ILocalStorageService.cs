namespace Limp.Client.Services.LocalStorageService
{
    public interface ILocalStorageService
    {
        Task<Guid> GetUserAgentIdAsync();
        Task<string?> ReadPropertyAsync(string key);
        Task WritePropertyAsync(string key, string value);
    }
}