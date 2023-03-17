using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs.UsersConnectedManaging.EventHandling
{
    public interface IUserConnectedHandler<T> where T : Hub
    {
        void OnConnect(string connectionId);
        void OnDisconnect(string connectionId, Func<Task>? callback = null);
        Task OnUsernameResolved
            (string connectionId,
            string accessToken, 
            Func<string, string, CancellationToken, Task>? AddUserToGroup = null, 
            Func<string, string, CancellationToken, Task>? CallOnMyNameResolveOnClient = null,
            Func<string, Task>? CallUserHubMethodsOnUsernameResolved = null);
    }
}
