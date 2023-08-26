using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Client.Pages.AccountManagement.LogicHandlers
{
    public interface ILoginHandler
    {
        void Dispose();
        Task OnLogIn(UserAuthentication loginEventInformation, Action<AuthResult> callback);
    }
}