using LimpShared.Models.ConnectedUsersManaging;

namespace Limp.Client.Services.HubServices.UndeliveredMessageSending
{
    public interface IUndeliveredMessageService
    {
        public void SubscribeToUsersOnlineUpdate();
        void Dispose();
    }
}