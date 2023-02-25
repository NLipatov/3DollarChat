using ClientServerCommon.Models;
using ClientServerCommon.Models.Login;
using ClientServerCommon.Models.Message;
using Limp.Client.TopicStorage;
using Limp.Client.Utilities;
using LimpShared.Authentification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubInteraction
{
    public class HubInteractor
    {
        public HubInteractor(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }
        private HubConnection? authHub;
        private HubConnection? usersHub;
        private HubConnection? messageDispatcherHub;
        private List<Guid> subscriptions = new();
        private readonly NavigationManager _navigationManager;

        public async Task<HubConnection> ConnectToAuthHubAsync(string accessToken, string refreshToken, Func<AuthResult, Task>? onTokensRefresh = null)
        {
            authHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/authHub"))
            .Build();

            authHub.On<AuthResult>("OnTokensRefresh", async result =>
            {
                if (onTokensRefresh != null)
                {
                    await onTokensRefresh(result);
                }
            });

            await authHub.StartAsync();

            if (TokenReader.HasAccessTokenExpired(accessToken))
            {
                await authHub.SendAsync("RefreshTokens", new RefreshToken { Token = refreshToken });
            }

            return authHub;
        }
        public async Task<HubConnection> ConnectToMessageDispatcherHubAsync(string accessToken, Action<Message>? onMessageReceive = null, Action<string>? onUsernameResolve = null, Action<Guid>? onMessageReceivedByRecepient = null)
        {
            if (onMessageReceive != null)
            {
                Guid subscriptionId = MessageBox.Subsctibe(onMessageReceive);
                subscriptions.Add(subscriptionId);
            }

            messageDispatcherHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/messageDispatcherHub"))
            .Build();

            messageDispatcherHub.On<Message>("ReceiveMessage", async message =>
            {
                MessageBox.AddMessage(message);
                if (message.Sender != "You")
                {
                    await messageDispatcherHub.SendAsync("MessageReceived", message.Id);
                }
            });

            messageDispatcherHub.On<Guid>("MessageWasReceivedByRecepient", messageId =>
            {
                if (onMessageReceivedByRecepient != null)
                {
                    onMessageReceivedByRecepient(messageId);
                }
            });

            if (onUsernameResolve != null)
            {
                messageDispatcherHub.On<string>("OnMyNameResolve", username =>
                {
                    onUsernameResolve(username);
                });
            }

            await messageDispatcherHub.StartAsync();

            await messageDispatcherHub.SendAsync("SetUsername", accessToken);

            return messageDispatcherHub;
        }

        public async Task<HubConnection> ConnectToUsersHubAsync(string accessToken, Action<string>? onConnectionIdReceive = null, Action<List<UserConnections>>? onOnlineUsersReceive = null, Func<string, Task>? onNameResolve = null)
        {
            usersHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
            .Build();

            usersHub.On<List<UserConnections>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                if (onOnlineUsersReceive != null)
                {
                    onOnlineUsersReceive(updatedTrackedUserConnections);
                }
            });

            usersHub.On<string>("ReceiveConnectionId", conId =>
            {
                if (onConnectionIdReceive != null)
                {
                    onConnectionIdReceive(conId);
                }
            });

            usersHub.On<string>("onNameResolve", username =>
            {
                if (onNameResolve != null)
                {
                    onNameResolve(username);
                }
            });

            await usersHub.StartAsync();

            await usersHub.SendAsync("SetUsername", accessToken);

            return usersHub;
        }

        public static List<Message> LoadStoredMessages(string topic)
        {
            return MessageBox.FetchMessagesFromMessageBox(topic);
        }

        public async Task SendMessage(Message message)
        {
            if (messageDispatcherHub != null)
                await messageDispatcherHub.SendAsync("Dispatch", message);
        }

        public bool IsMessageHubConnected()
        {
            if (messageDispatcherHub == null)
                return false;

            return messageDispatcherHub.State == HubConnectionState.Connected;
        }

        public async Task DisposeAsync()
        {
            if (usersHub != null)
            {
                await usersHub.DisposeAsync();
            }
            if (messageDispatcherHub != null)
            {
                await messageDispatcherHub.DisposeAsync();
            }
            if (authHub != null)
            {
                await authHub.DisposeAsync();
            }
            foreach (var subscription in subscriptions)
            {
                MessageBox.Unsubscribe(subscription);
            }
        }
    }
}
