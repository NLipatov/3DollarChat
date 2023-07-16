using Limp.Client.ClientOnlyModels;
using LimpShared.Models.ConnectedUsersManaging;

namespace Limp.Client.Services.HubServices.UndeliveredMessageSending
{
    public interface IUndeliveredMessageService
    {
        Task SendUndelivered(ClientMessage message);
        Task SendUndelivered(string username);
        public void SubscribeToUsersOnlineUpdate();
        void Dispose();
    }
}