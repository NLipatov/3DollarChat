using LimpShared.Models.Message;
using LimpShared.Models.Message.DataTransfer;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        Task ReconnectAsync();
        bool IsConnected();
        Task SendMessage(Message message);
        Task NegotiateOnAESAsync(string partnerUsername);
        Task SendData(List<DataFile> files, string targetGroup);
        Task SendData(Guid fileId, string targetGroup);
        Task SendText(string text, string targetGroup, string myUsername);
        Task RequestPartnerToDeleteConvertation(string targetGroup);
        Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername);
    }
}
