using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.UserAuthentication;

namespace Limp.Client.Pages.AccountManagement.LogicHandlers
{
    public interface ILoginHandler
    {
        void Dispose();
        Task OnLogIn(UserAuthentication loggingInUser, Action<AuthResult> callback);
    }
}