using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.EventTypes;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.HubInteraction.HubObservers;
using Limp.Client.TopicStorage;
using LimpShared.Authentification;
using LimpShared.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Limp.Client.HubConnectionManagement.ConnectionHandlers.HubInteraction.Implementations
{
    public class MessageDispatcherHubInteractor : IHubInteractor<MessageDispatcherHubInteractor>
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jSRuntime;
        private readonly IHubObserver<MessageHubEvent> _messageDispatcherHubObserver;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAESOfferHandler _aESOfferHandler;
        private readonly IMessageBox _messageBox;
        private readonly HubConnection _usersHub;
        private readonly HubConnection _authHub;
        private HubConnection? messageDispatcherHub;
        private string myName = string.Empty;
        private Guid? messageBoxSubscriptionId;
        public MessageDispatcherHubInteractor
        (NavigationManager navigationManager,
        IJSRuntime jSRuntime,
        IHubObserver<MessageHubEvent> messageDispatcherHubObserver,
        ICryptographyService cryptographyService,
        IAESOfferHandler aESOfferHandler,
        IMessageBox messageBox,
        HubConnection usersHub,
        HubConnection authHub)
        {
            _navigationManager = navigationManager;
            _jSRuntime = jSRuntime;
            _messageDispatcherHubObserver = messageDispatcherHubObserver;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _messageBox = messageBox;
            _usersHub = usersHub;
            _authHub = authHub;
        }
        private async Task<string?> GetAccessToken()
            => await _jSRuntime.InvokeAsync<string>("localStorage.getItem", "access-token");
        public async Task<HubConnection> ConnectAsync()
        {
            Guid subscriptionId = _messageBox.Subsctibe(OnMessageReceived);
            messageBoxSubscriptionId = subscriptionId;

            messageDispatcherHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/messageDispatcherHub"))
            .Build();

            #region Event handlers registration
            messageDispatcherHub.On<List<UserConnection>>("ReceiveOnlineUsers", async updatedTrackedUserConnections =>
            {
                await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.OnlineUsersReceived, updatedTrackedUserConnections);
            });

            messageDispatcherHub.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    if (message.Type == MessageType.AESAccept)
                    {
                        await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.AESAccept, message);
                        return;
                    }

                    if (_cryptographyService == null)
                        throw new ArgumentException($"Please provide an instance of type {typeof(ICryptographyService)} as an argument.");

                    if (message.Type == MessageType.AESOffer)
                    {
                        await SendMessage(await _aESOfferHandler.GetAESOfferResponse(message));
                    }
                }

                await _messageBox.AddMessageAsync(message);

                await messageDispatcherHub.SendAsync("MessageReceived", message.Id);

                //If we dont yet know a partner Public Key, we will request it from server side.
                await GetPartnerPublicKey(message.Sender!);
            });

            messageDispatcherHub.On<Guid>("MessageWasReceivedByRecepient", async messageId =>
            {
                await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.MessageReceivedByRecepient, messageId);
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
                await GenerateAESAndSendItToPartner(_cryptographyService!, partnersUsername, partnersPublicKey);
            });

            messageDispatcherHub.On<string>("OnMyNameResolve", async username =>
            {
                myName = username;
                await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.MyUsernameResolved, myName);
                await UpdateRSAPublicKeyAsync(await GetAccessToken(), InMemoryKeyStorage.MyRSAPublic!);
            });

            messageDispatcherHub.On<TokenRelatedOperationResult>("OnFailedTokenRelatedOperation", async failedOperationDetails =>
            {
                await Console.Out.WriteLineAsync(failedOperationDetails.Username);
                await _authHub!.SendAsync("RefreshTokens", new RefreshToken
                {
                    Token = (await JWTHelper.GetRefreshToken(_jSRuntime))!
                });
            });
            #endregion

            await messageDispatcherHub.StartAsync();
            await messageDispatcherHub.SendAsync("SetUsername", await GetAccessToken());

            return messageDispatcherHub;
        }

        private async Task OnMessageReceived(Message message)
        {
            await _messageDispatcherHubObserver.CallHandler(MessageHubEvent.MessageReceived, message);
        }

        private async Task SendMessage(Message message)
        {
            if (messageDispatcherHub != null)
                await messageDispatcherHub.SendAsync("Dispatch", message);
        }

        private async Task GetPartnerPublicKey(string partnersUsername)
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
                _usersHub?.SendAsync("SetRSAPublicKey", accessToken, RSAPublicKey);
                InMemoryKeyStorage.isPublicKeySet = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if(_messageDispatcherHubObserver != null)
            {
                _messageDispatcherHubObserver.UnsubscriveAll();
            }
            if (messageDispatcherHub != null)
            {
                await messageDispatcherHub.StopAsync();
                await messageDispatcherHub.DisposeAsync();
            }
            if(messageBoxSubscriptionId != null)
            {
                _messageBox.Unsubscribe(messageBoxSubscriptionId.Value);
            }
        }
    }
}
