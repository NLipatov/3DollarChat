using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.TopicStorage;
using LimpShared.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubInteraction
{
    public class HubInteractor
    {
        private HubConnection? usersHub;
        private HubConnection? messageDispatcherHub;
        private List<Guid> subscriptions = new();
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jSRuntime;
        private readonly IMessageBox _messageBox;
        private readonly IAESOfferHandler _aesOfferHandler;
        private string myName = string.Empty;

        public HubInteractor
        (NavigationManager navigationManager,
        IJSRuntime jSRuntime,
        IMessageBox messageBox,
        IAESOfferHandler aESOfferHandler)
        {
            _navigationManager = navigationManager;
            _jSRuntime = jSRuntime;
            _messageBox = messageBox;
            _aesOfferHandler = aESOfferHandler;
        }

        private async Task<string?> GetAccessToken()
            => await _jSRuntime.InvokeAsync<string>("localStorage.getItem", "access-token");

        public async Task<HubConnection> ConnectToMessageDispatcherHubAsync
        (Func<Message, Task>? onMessageReceive = null,
        Func<string, Task>? onUsernameResolve = null,
        Action<Guid>? onMessageReceivedByRecepient = null,
        ICryptographyService? cryptographyService = null,
        Func<Task>? OnAESAcceptedCallback = null,
        Action<List<UserConnections>>? onOnlineUsersReceive = null)
        {
            if (onMessageReceive != null)
            {
                Guid subscriptionId = _messageBox.Subsctibe(onMessageReceive);
                subscriptions.Add(subscriptionId);
            }

            messageDispatcherHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/messageDispatcherHub"))
            .Build();

            messageDispatcherHub.On<List<UserConnections>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                if (onOnlineUsersReceive != null)
                {
                    onOnlineUsersReceive(updatedTrackedUserConnections);
                }
            });

            messageDispatcherHub.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    if (message.Type == MessageType.AESAccept)
                    {
                        if (OnAESAcceptedCallback != null)
                        {
                            await OnAESAcceptedCallback();
                        }
                        return;
                    }

                    if (cryptographyService == null)
                        throw new ArgumentException($"Please provide an instance of type {typeof(ICryptographyService)} as an argument.");

                    if (message.Type == MessageType.AESOffer)
                    {
                        await SendMessage(await _aesOfferHandler.GetAESOfferResponse(message));
                    }
                }

                await _messageBox.AddMessageAsync(message);

                await messageDispatcherHub.SendAsync("MessageReceived", message.Id);

                //If we dont yet know a partner Public Key, we will request it from server side.
                await GetPartnerPublicKey(message.Sender!);
            });

            messageDispatcherHub.On<Guid>("MessageWasReceivedByRecepient", messageId =>
            {
                if (onMessageReceivedByRecepient != null)
                {
                    onMessageReceivedByRecepient(messageId);
                }
            });

            //Handling server side response on partners Public Key
            messageDispatcherHub.On<string, string>("ReceivePublicKey", async (partnersUsername, partnersPublicKey) =>
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
                await GenerateAESAndSendItToPartner(cryptographyService!, partnersUsername, partnersPublicKey);
            });

            if (onUsernameResolve != null)
            {
                messageDispatcherHub.On<string>("OnMyNameResolve", async username =>
                {
                    myName = username;
                    await onUsernameResolve(username);
                    await UpdateRSAPublicKeyAsync(await GetAccessToken(), InMemoryKeyStorage.MyRSAPublic!);
                });
            }

            await messageDispatcherHub.StartAsync();

            await messageDispatcherHub.SendAsync("SetUsername", await GetAccessToken());

            return messageDispatcherHub;
        }
        public async Task GetPartnerPublicKey(string partnersUsername)
        {
            if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == partnersUsername).Value == null)
            {
                await messageDispatcherHub.SendAsync("GetAnRSAPublic", partnersUsername);
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

                await SendMessage(offerOnAES);
            });
        }

        public async Task UpdateRSAPublicKeyAsync(string accessToken, Key RSAPublicKey)
        {
            if (!InMemoryKeyStorage.isPublicKeySet)
            {
                usersHub?.SendAsync("SetRSAPublicKey", accessToken, RSAPublicKey);
                InMemoryKeyStorage.isPublicKeySet = true;
            }
        }

        public List<Message> LoadStoredMessages(string topic)
        {
            return _messageBox.FetchMessagesFromMessageBox(topic);
        }

        public async Task SendMessage(Message message)
        {
            if (messageDispatcherHub != null)
                await messageDispatcherHub.SendAsync("Dispatch", message);
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
            foreach (var subscription in subscriptions)
            {
                _messageBox.Unsubscribe(subscription);
            }
        }
    }
}
