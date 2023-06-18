using Limp.Client.ClientOnlyModels;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Limp.Client.Services.UndeliveredMessagesStore.Implementation
{
    public class UndeliveredMessagesRepository : IUndeliveredMessagesRepository
    {
        private const string localStorageObjectName = nameof(UndeliveredMessagesRepository);
        private readonly IJSRuntime _jSRuntime;

        public UndeliveredMessagesRepository(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task AddAsync(ClientMessage messages)
        {
            List<ClientMessage> undelivered = await GetUndeliveredAsync();

            undelivered.Add(messages);

            await SaveUndeliveredListAsync(undelivered);
        }

        public async Task AddRange(List<ClientMessage> messages)
        {
            List<ClientMessage> undelivered = await GetUndeliveredAsync();

            undelivered.AddRange(messages);

            await SaveUndeliveredListAsync(undelivered);
        }

        public async Task DeleteAsync(Guid messageId)
        {
            List<ClientMessage> undelivered = await GetUndeliveredAsync();

            ClientMessage? message = undelivered.FirstOrDefault(x => x.Id == messageId);
            if(message != null)
            {
                undelivered.Remove(message);
                await SaveUndeliveredListAsync(undelivered);
            }
        }

        public async Task DeleteRangeAsync(Guid[] messageIds)
        {
            List<ClientMessage> undelivered = await GetUndeliveredAsync();

            List<ClientMessage> messages = undelivered.Where(x=>messageIds.Any(id => id == x.Id)).ToList();

            await SaveUndeliveredListAsync(messages);
        }

        public async Task<List<ClientMessage>> GetUndeliveredAsync()
        {
            string? undeliveredMessagesSerialized = await _jSRuntime
                .InvokeAsync<string?>("localStorage.getItem", localStorageObjectName);

            if(string.IsNullOrWhiteSpace(undeliveredMessagesSerialized))
                return new(0);

            List<ClientMessage>? undeliveredMessages = JsonSerializer.Deserialize<List<ClientMessage>>(undeliveredMessagesSerialized);

            return undeliveredMessages ?? new(0);
        }

        public async Task SaveUndeliveredListAsync(List<ClientMessage> messages)
        {
            string undeliveredMessagesSerialized = JsonSerializer.Serialize(messages);

            await _jSRuntime.InvokeVoidAsync("localStorage.setItem", localStorageObjectName, undeliveredMessagesSerialized);
        }
    }
}
