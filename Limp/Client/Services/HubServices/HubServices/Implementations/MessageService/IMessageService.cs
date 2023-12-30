using Limp.Client.ClientOnlyModels;
using LimpShared.Models.Message.DataTransfer;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.MessageService
{
    public interface IMessageService : IHubService
    {
        bool IsConnected();
        Task NegotiateOnAESAsync(string partnerUsername);
        Task SendText(string text, string targetGroup, string myUsername);
        Task SendMessage(ClientMessage message);
        Task RequestPartnerToDeleteConvertation(string targetGroup);
        Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername);
    }
}
