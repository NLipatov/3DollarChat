using LimpShared.Models.Message;

namespace Limp.Client.Services.UndeliveredMessagesStore
{
    public interface IUndeliveredMessagesRepository
    {
        Task DeleteAsync(Guid messageId);
        Task DeleteRangeAsync(Guid[] messageIds);
        Task AddRange(List<Message> messages);
        Task AddAsync(Message messages);
        Task<List<Message>> GetUndeliveredAsync();
    }
}
