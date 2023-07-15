using LimpShared.Models.ConnectedUsersManaging;

namespace Limp.Client.Services.HubServices.UndeliveredMessageSending
{
    public interface IUndeliveredMessageService
    {
        Task SendUndelivered(string username);
        public void SubscribeToUsersOnlineUpdate();
        void Dispose();
    }
}