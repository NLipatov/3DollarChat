using LimpShared.Models.Authentication.Models;
using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.AuthenticationService.Handlers;

public interface IAuthenticationHandler
{
    Task<ICredentials> GetCredentials();
    Task<AuthenticationType?> GetAuthenticationTypeAsync();
    Task<string> GetRefreshCredential();
    Task<string> GetAccessCredential();
    Task<string> GetUsernameAsync();
    Task<bool> IsSetToUseAsync();
    Task TriggerCredentialsValidation(HubConnection hubConnection);
}