using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.UserAuthentication;

namespace Ethachat.Client.Pages.AccountManagement.LogicHandlers
{
    public interface ILoginHandler
    {
        void Dispose();
        Task OnLogIn(UserAuthentication loginEventInformation, Action<AuthResult> callback);
    }
}