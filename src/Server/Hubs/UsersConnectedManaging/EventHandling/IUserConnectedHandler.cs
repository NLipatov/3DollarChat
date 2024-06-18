using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;
using Microsoft.AspNetCore.SignalR;

namespace Ethachat.Server.Hubs.UsersConnectedManaging.EventHandling
{
    public interface IUserConnectedHandler<T> where T : Hub
    {
        void OnConnect(string connectionId);
        void OnDisconnect
        (string connectionId,
        Func<string, string, CancellationToken, Task>? RemoveUserFromGroup = null);
        Task OnUsernameResolved
        (string connectionId,
        string username,
        Func<string, string, CancellationToken, Task>? AddToUserGroup = null, 
        Func<string, string, CancellationToken, Task>? SendToCaller = null,
        Func<string, TokenRelatedOperationResult, CancellationToken, Task>? OnFaultTokenRelatedOperation = null,
        Func<string, Task>? CallUserHubMethodsOnUsernameResolved = null,
        WebAuthnPair? webAuthnPair = null,
        JwtPair? jwtPair = null);
    }
}
