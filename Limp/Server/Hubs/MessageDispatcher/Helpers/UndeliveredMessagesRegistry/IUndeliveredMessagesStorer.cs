using ClientServerCommon.Models.Message;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.UndeliveredMessagesRegistry
{
    public interface IUndeliveredMessagesStorer
    {
        void Add(Message message);
        void Remove(Guid messageId, string targetGroup);
    }
}