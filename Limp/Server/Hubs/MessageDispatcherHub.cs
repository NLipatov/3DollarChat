using Limp.Server.Hubs.UserStorage;
using Limp.Server.Utilities.HttpMessaging;
using Limp.Shared.Models;
using LimpShared.Authentification;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class MessageDispatcherHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        public MessageDispatcherHub(IServerHttpClient serverHttpClient)
        {
            _serverHttpClient = serverHttpClient;
        }
        public async Task Dispatch(Message message)
        {
            string targetGroup = message.TargetGroup;

            await Clients.Group(targetGroup).SendAsync("ReceiveMessage", message);
        }

        public async Task SetUsername(string accessToken)
        {
            TokenRelatedOperationResult usernameRequest = await _serverHttpClient.GetUserNameFromAccessTokenAsync(accessToken);

            var username = usernameRequest.Username;

            if (InMemoryUsersStorage.UserConnections.Any(x => x.Username == username))
            {
                InMemoryUsersStorage.UserConnections.First(x => x.Username == username).ConnectionIds.Add(Context.ConnectionId);

                foreach (var connection in InMemoryUsersStorage.UserConnections.First(x => x.Username == username).ConnectionIds)
                {
                    await Groups.AddToGroupAsync(connection, username);
                }
            }
            else
            {
                InMemoryUsersStorage
                    .UserConnections
                    .First(x => x.ConnectionIds.Contains(Context.ConnectionId)).Username = username;
            }

            await Clients.Caller.SendAsync("OnMyNameResolve", username);
        }
    }
}