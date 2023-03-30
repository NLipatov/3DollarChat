using ClientServerCommon.Models.Message;

namespace Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.MessageHubEventHandlers
{
    public interface IEventHandler<T>
    {
        Task Handle(T parameter);
    }
}
