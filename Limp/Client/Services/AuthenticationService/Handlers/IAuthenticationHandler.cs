using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Authentication.Types;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.AuthenticationService.Handlers;

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
    Task ExecutePostCredentialsValidation(AuthResult result, HubConnection hubConnection);
}