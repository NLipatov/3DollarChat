using LimpShared.Models.Authentication.Models.Credentials;
using LimpShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using LimpShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.AuthenticationService.Handlers;

public interface IAuthenticationHandler
{
    Task<CredentialsDTO> GetCredentialsDto();
    Task<ICredentials> GetCredentials();
    Task<AuthenticationType?> GetAuthenticationTypeAsync();
    Task<string> GetRefreshCredential();
    Task<string> GetAccessCredential();
    Task<string> GetUsernameAsync();
    Task<bool> IsSetToUseAsync();
    Task TriggerCredentialsValidation(HubConnection hubConnection);
    Task UpdateCredentials(ICredentials newCredentials);
}