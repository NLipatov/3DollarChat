using ClientServerCommon.Models.Message;
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

        public async Task AddAsync(Message messages)
        {
            List<Message> undelivered = await GetUndeliveredAsync();

            undelivered.Add(messages);

            await SaveUndeliveredListAsync(undelivered);
        }

        public async Task AddRange(List<Message> messages)
        {
            List<Message> undelivered = await GetUndeliveredAsync();

            undelivered.AddRange(messages);

            await SaveUndeliveredListAsync(undelivered);
        }

        public async Task DeleteAsync(Guid messageId)
        {
            List<Message> undelivered = await GetUndeliveredAsync();

            Message? message = undelivered.FirstOrDefault(x => x.Id == messageId);
            if(message != null)
            {
                undelivered.Remove(message);
                await SaveUndeliveredListAsync(undelivered);
            }
        }

        public async Task DeleteRangeAsync(Guid[] messageIds)
        {
            List<Message> undelivered = await GetUndeliveredAsync();

            List<Message> messages = undelivered.Where(x=>messageIds.Any(id => id == x.Id)).ToList();

            await SaveUndeliveredListAsync(messages);
        }

        public async Task<List<Message>> GetUndeliveredAsync()
        {
            string? undeliveredMessagesSerialized = await _jSRuntime
                .InvokeAsync<string?>("localStorage.getItem", localStorageObjectName);

            if(string.IsNullOrWhiteSpace(undeliveredMessagesSerialized))
                return new(0);

            List<Message>? undeliveredMessages = JsonSerializer.Deserialize<List<Message>>(undeliveredMessagesSerialized);

            return undeliveredMessages ?? new(0);
        }

        public async Task SaveUndeliveredListAsync(List<Message> messages)
        {
            string undeliveredMessagesSerialized = JsonSerializer.Serialize(messages);

            await _jSRuntime.InvokeVoidAsync("localStorage.setItem", localStorageObjectName, undeliveredMessagesSerialized);
        }
    }
}
