using Microsoft.JSInterop;

namespace Limp.Client.Services.LocalStorageService
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _jSRuntime;

        private const string userAgentObjectName = "userAgent";

        public LocalStorageService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        public async Task<string?> ReadPropertyAsync(string key)
            => await _jSRuntime.InvokeAsync<string?>("localStorage.getItem", key);

        public async Task WritePropertyAsync(string key, string value)
            => await _jSRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

        public async Task<Guid> GetUserAgentIdAsync()
        {
            //Trying to read user agent id from local storage
            string? userAgent = await ReadPropertyAsync(userAgentObjectName);

            //Trying to parse user agent id string readed from local storage
            if (Guid.TryParse(userAgent, out var userAgentId))
            {
                //Return if parsed
                return userAgentId;
            }
            else
            {
                //Create new, if not parsed
                userAgentId = Guid.NewGuid();
                await WritePropertyAsync(userAgentObjectName, userAgentId.ToString());
                return userAgentId;
            }
        }
    }
}
