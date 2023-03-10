using ClientServerCommon.Models;
using ClientServerCommon.Models.Login;
using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.TopicStorage;
using Limp.Client.Utilities;
using LimpShared.Authentification;
using LimpShared.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction
{
    public class HubInteractor
    {
        private HubConnection? authHub;
        private HubConnection? usersHub;
        private HubConnection? messageDispatcherHub;
        private List<Guid> subscriptions = new();
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jSRuntime;

        public HubInteractor
            (NavigationManager navigationManager,
            IJSRuntime jSRuntime)
        {
            _navigationManager = navigationManager;
            _jSRuntime = jSRuntime;
        }

        private async Task<string?> GetAccessToken() 
            => await _jSRuntime.InvokeAsync<string>("localStorage.getItem", "access-token");

        private async Task<string?> GetRefreshToken()
            => await _jSRuntime.InvokeAsync<string>("localStorage.getItem", "refresh-token");

        public async Task<HubConnection> ConnectToAuthHubAsync
        (Func<AuthResult, Task>? onTokensRefresh = null)
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

            if (TokenReader.HasAccessTokenExpired(await GetAccessToken()))
            {
                await authHub.SendAsync("RefreshTokens", new RefreshToken { Token = await GetRefreshToken() });
            }

            return authHub;
        }
        
        public async Task<HubConnection> ConnectToMessageDispatcherHubAsync
        (Action<Message>? onMessageReceive = null, 
        Action<string>? onUsernameResolve = null, 
        Action<Guid>? onMessageReceivedByRecepient = null)
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
                    if(InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x=>x.Key == message.TargetGroup).Value == null)
                    {
                        await messageDispatcherHub.SendAsync("GetAnRSAPublic", message.Sender);
                    }
                }
            });

            messageDispatcherHub.On<Guid>("MessageWasReceivedByRecepient", messageId =>
            {
                if (onMessageReceivedByRecepient != null)
                {
                    onMessageReceivedByRecepient(messageId);
                }
            });

            messageDispatcherHub.On<string, string>("ReceivePublicKey", (username, key) =>
            {
                InMemoryKeyStorage.AESKeyStorage.Add(username, new Key
                {
                    Type = KeyType.RSAPublic,
                    Contact = username,
                    Format = KeyFormat.PEM_SPKI,
                    Value = key
                });
            });

            if (onUsernameResolve != null)
            {
                messageDispatcherHub.On<string>("OnMyNameResolve", async username =>
                {
                    onUsernameResolve(username);
                    await UpdateRSAPublicKeyAsync(await GetAccessToken(), InMemoryKeyStorage.MyRSAPublic);
                });
            }

            await messageDispatcherHub.StartAsync();

            await messageDispatcherHub.SendAsync("SetUsername", await GetAccessToken());

            return messageDispatcherHub;
        }

        public async Task<HubConnection> ConnectToUsersHubAsync
        (Action<string>? onConnectionIdReceive = null,
        Action<List<UserConnections>>? onOnlineUsersReceive = null,
        Func<string, Task>? onNameResolve = null,
        Func<string, Task>? onPartnerRSAPublicKeyReceived = null)
        {
            usersHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/usersHub"))
            .Build();

            usersHub.On<string>("ReceivePartnerRSAPublicKey", async PEMEncodedKey =>
            {
                if (onPartnerRSAPublicKeyReceived != null)
                    await onPartnerRSAPublicKeyReceived(PEMEncodedKey);
            });

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

            usersHub.On<string>("onNameResolve", async username =>
            {
                if (onNameResolve != null)
                {
                    await onNameResolve(username);
                    await UpdateRSAPublicKeyAsync(await GetAccessToken(), InMemoryKeyStorage.MyRSAPublic);
                }
            });

            await usersHub.StartAsync();

            await usersHub.SendAsync("SetUsername", await GetAccessToken());

            return usersHub;
        }

        public async Task UpdateRSAPublicKeyAsync(string accessToken, Key RSAPublicKey)
        {
            if (!InMemoryKeyStorage.isPublicKeySet)
            {
                usersHub?.SendAsync("SetRSAPublicKey", accessToken, RSAPublicKey);
                InMemoryKeyStorage.isPublicKeySet = true;
            }
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
