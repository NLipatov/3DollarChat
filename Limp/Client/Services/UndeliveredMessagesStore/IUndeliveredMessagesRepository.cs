using Limp.Client.ClientOnlyModels;
using LimpShared.Models.Message;

namespace Limp.Client.Services.UndeliveredMessagesStore
{
    public interface IUndeliveredMessagesRepository
    {
        Task DeleteAsync(Guid messageId);
        Task DeleteRangeAsync(Guid[] messageIds);
        Task AddRange(List<ClientMessage> messages);
        Task AddAsync(ClientMessage messages);
        Task<List<ClientMessage>> GetUndeliveredAsync();
    }
}
