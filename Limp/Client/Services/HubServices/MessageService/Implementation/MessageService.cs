using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.Services.HubService.CommonServices;
using Limp.Client.Services.HubService.UsersService;
using Limp.Client.TopicStorage;
using LimpShared.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Limp.Client.Services.HubServices.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        private readonly IMessageBox _messageBox;
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAESOfferHandler _aESOfferHandler;
        private readonly IUsersService _usersService;
        private ConcurrentDictionary<Guid, Func<Message, Task>> OnMessageReceiveCallbacks = new();
        private ConcurrentDictionary<Guid, Func<List<UserConnection>, Task>> OnUsersOnlineUpdateCallbacks = new();
        private string myName;

        private HubConnection? HubConnection { get; set; }

        public MessageService
        (IMessageBox messageBox,
        IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        ICryptographyService cryptographyService,
        IAESOfferHandler aESOfferHandler,
        IUsersService usersService)
        {
            _messageBox = messageBox;
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _usersService = usersService;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            HubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/messageDispatcherHub"))
            .Build();

            #region Event handlers registration
            HubConnection.On<List<UserConnection>>("ReceiveOnlineUsers", async updatedTrackedUserConnections =>
            {
                CallbackExecutor.ExecuteCallbackDictionary(updatedTrackedUserConnections, OnUsersOnlineUpdateCallbacks);
            });

            HubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    if (message.Type == MessageType.AESAccept)
                    {
                        //await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.AESAccept, message);
                        return;
                    }

                    if (_cryptographyService == null)
                        throw new ArgumentException($"Please provide an instance of type {typeof(ICryptographyService)} as an argument.");

                    if (message.Type == MessageType.AESOffer)
                    {
                        if (HubConnection != null)
                            await HubConnection.SendAsync("Dispatch", await _aESOfferHandler.GetAESOfferResponse(message));
                    }
                }

                await _messageBox.AddMessageAsync(message);

                await HubConnection.SendAsync("MessageReceived", message.Id);

                //If we dont yet know a partner Public Key, we will request it from server side.
                if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == message.Sender).Value == null)
                {
                    await HubConnection.SendAsync("GetAnRSAPublic", message.Sender);
                }
            });

            HubConnection.On<Guid>("MessageWasReceivedByRecepient", async messageId =>
            {
                //await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.MessageReceivedByRecepient, messageId);
            });

            //Handling server side response on partners Public Key
            HubConnection.On<string, string>("ReceivePublicKey", async (partnersUsername, partnersPublicKey) =>
            {
                if (partnersUsername == "You")
                    return;
                //Storing Public Key in our in-memory storage
                InMemoryKeyStorage.RSAKeyStorage.TryAdd(partnersUsername, new Key
                {
                    Type = KeyType.RSAPublic,
                    Contact = partnersUsername,
                    Format = KeyFormat.PEM_SPKI,
                    Value = partnersPublicKey
                });

                //Now we can send an encrypted offer on AES Key
                //We will encrypt our offer with a partners RSA Public Key
                await GenerateAESAndSendItToPartner(_cryptographyService!, partnersUsername, partnersPublicKey);
            });

            HubConnection.On<string>("OnMyNameResolve", async username =>
            {
                myName = username;
                //await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.MyUsernameResolved, myName);
                await UpdateRSAPublicKeyAsync(await JWTHelper.GetAccessToken(_jSRuntime), InMemoryKeyStorage.MyRSAPublic!);
            });

            #endregion

            await HubConnection.StartAsync();
            await HubConnection.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

            return HubConnection;
        }
        public async Task UpdateRSAPublicKeyAsync(string accessToken, Key RSAPublicKey)
        {
            if (!InMemoryKeyStorage.isPublicKeySet)
            {
                await _usersService.SetRSAPublicKey(accessToken, RSAPublicKey);
            }
        }
        private async Task GenerateAESAndSendItToPartner
        (ICryptographyService cryptographyService,
        string partnersUsername,
        string partnersPublicKey)
        {
            if (InMemoryKeyStorage.AESKeyStorage.FirstOrDefault(x => x.Key == partnersUsername).Value != null)
            {
                return;
            }

            await cryptographyService.GenerateAESKeyAsync(partnersUsername, async (aesKeyForConversation) =>
            {
                string? offeredAESKeyForConversation = InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value!.ToString();

                if (string.IsNullOrWhiteSpace(offeredAESKeyForConversation))
                    throw new ApplicationException("Could not properly generated an AES Key for conversation");

                await Console.Out.WriteLineAsync($"AES for {partnersUsername}: {offeredAESKeyForConversation}.");

                //When this callback is called, AES key for conversation is already generated
                //We now need to encrypt this AES key and send it to partner
                string? encryptedAESKey = (await cryptographyService
                .EncryptAsync<RSAHandler>
                    (new Cryptogramm { Cyphertext = offeredAESKeyForConversation },
                    //We will encrypt it with partners Public Key, so he will be able to decrypt it with his Private Key
                    PublicKeyToEncryptWith: partnersPublicKey)).Cyphertext;

                if (string.IsNullOrWhiteSpace(encryptedAESKey))
                    throw new ApplicationException("Could not encrypt a AES Key, got empty string.");

                Message offerOnAES = new()
                {
                    Type = MessageType.AESOffer,
                    DateSent = DateTime.UtcNow,
                    Sender = myName,
                    TargetGroup = partnersUsername,
                    Payload = encryptedAESKey
                };

                if (HubConnection != null)
                    await HubConnection.SendAsync("Dispatch", offerOnAES);
            });
        }

        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public Guid SubscribeToUsersOnline(Func<List<UserConnection>, Task> callback)
        {
            Guid subscriptionId = Guid.NewGuid();
            bool isAdded = OnUsersOnlineUpdateCallbacks.TryAdd(subscriptionId, callback);
            if (!isAdded)
            {
                SubscribeToUsersOnline(callback);
            }
            return subscriptionId;
        }

        public void RemoveSubscriptionToUsersOnline(Guid subscriptionId)
        {
            bool isRemoved = OnUsersOnlineUpdateCallbacks.Remove(subscriptionId, out _);
            if (!isRemoved)
            {
                RemoveSubscriptionToUsersOnline(subscriptionId);
            }
        }
    }
}
