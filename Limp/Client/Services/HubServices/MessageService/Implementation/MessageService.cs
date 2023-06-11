using ClientServerCommon.Models.HubMessages;
using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Pages.PersonalChat.Logic.MessageBuilder;
using Limp.Client.Services.CloudKeyService;
using Limp.Client.Services.HubService.UsersService;
using Limp.Client.Services.HubServices.CommonServices;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.InboxService;
using Limp.Client.Services.UndeliveredMessagesStore;
using LimpShared.Encryption;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

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
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IUndeliveredMessagesRepository _undeliveredMessagesRepository;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IBrowserKeyStorage _localKeyManager;
        private string myName;
        public bool IsConnected() => hubConnection?.State == HubConnectionState.Connected;

        private HubConnection? hubConnection { get; set; }

        public MessageService
        (IMessageBox messageBox,
        IJSRuntime jSRuntime,
        NavigationManager navigationManager,
        ICryptographyService cryptographyService,
        IAESOfferHandler aESOfferHandler,
        IUsersService usersService,
        ICallbackExecutor callbackExecutor,
        IUndeliveredMessagesRepository undeliveredMessagesRepository,
        IMessageBuilder messageBuilder,
        IBrowserKeyStorage localKeyManager)
        {
            _messageBox = messageBox;
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _undeliveredMessagesRepository = undeliveredMessagesRepository;
            _messageBuilder = messageBuilder;
            _localKeyManager = localKeyManager;
        }
        public async Task<HubConnection> ConnectAsync()
        {
            HubConnection? existingHubConnection = await TryGetExistingHubConnection();
            if (existingHubConnection != null)
            {
                return existingHubConnection;
            }

            hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/messageDispatcherHub"))
            .Build();

            #region Event handlers registration
            hubConnection.On<UsersOnlineMessage>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
            });

            hubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    if (hubConnection == null || hubConnection.State != HubConnectionState.Connected)
                        throw new ApplicationException($"{nameof(hubConnection)} is not useable.");

                    await hubConnection.SendAsync("MessageReceived", message.Id, message.Sender);

                    if (message.Type == MessageType.AESAccept)
                    {
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
                        _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        InMemoryKeyStorage.AESKeyStorage[message.Sender!].IsAccepted = true;
                        return;
                    }

                    if (_cryptographyService == null)
                        throw new ArgumentException($"Please provide an instance of type {typeof(ICryptographyService)} as an argument.");

                    if (message.Type == MessageType.AESOffer)
                    {
                        if (hubConnection != null)
                        {
                            await hubConnection.SendAsync("Dispatch", await _aESOfferHandler.GetAESOfferResponse(message));
                            _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        }
                    }
                }

                await _messageBox.AddMessageAsync(message);

                //If we dont yet know a partner Public Key and we dont have an AES Key for chat with partner,
                //we will request it from server side.
                if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == message.Sender).Value == null
                &&
                _localKeyManager.GetAESKeyForChat(message.Sender!) == null)
                {
                    if (hubConnection == null)
                    {
                        await ReconnectAsync();
                    }
                    else
                        await hubConnection.SendAsync("GetAnRSAPublic", message.Sender);
                }
            });

            hubConnection.On<Guid>("OnReceiverMarkedMessageAsReceived", messageId =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnReceiverMarkedMessageAsReceived");
            });

            //Handling server side response on partners Public Key
            hubConnection.On<string, string>("ReceivePublicKey", async (partnersUsername, partnersPublicKey) =>
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

            hubConnection.On<string>("OnMyNameResolve", async username =>
            {
                myName = username;
                string? accessToken = await JWTHelper.GetAccessToken(_jSRuntime);
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _navigationManager.NavigateTo("/login");
                    return;
                }
                if (string.IsNullOrWhiteSpace(InMemoryKeyStorage.MyRSAPublic?.Value?.ToString()))
                {
                    throw new ApplicationException("RSA Public key was not properly generated.");
                }

                await UpdateRSAPublicKeyAsync(accessToken, InMemoryKeyStorage.MyRSAPublic);
            });

            #endregion

            await hubConnection.StartAsync();
            await hubConnection.SendAsync("SetUsername", await JWTHelper.GetAccessToken(_jSRuntime));

            return hubConnection;
        }
        private async Task<HubConnection?> TryGetExistingHubConnection()
        {
            if (hubConnection != null)
            {
                if (hubConnection.State != HubConnectionState.Connected)
                {
                    await hubConnection.StopAsync();
                    await hubConnection.StartAsync();
                }
                return hubConnection;
            }
            return null;
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
            await cryptographyService.GenerateAESKeyAsync(partnersUsername, async (aesKeyForConversation) =>
            {
                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.CreationDate = DateTime.UtcNow;
                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value = aesKeyForConversation;
                await _localKeyManager.SaveInMemoryKeysInLocalStorage();
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
                    PlainTextPayload = encryptedAESKey
                };

                if (hubConnection != null)
                    await hubConnection.SendAsync("Dispatch", offerOnAES);
            });
        }

        public async Task DisconnectAsync()
        {
            await HubDisconnecter.DisconnectAsync(hubConnection);
            hubConnection = null;
        }

        public async Task RequestForPartnerPublicKey(string partnerUsername)
        {
            if (hubConnection != null)
            {
                await hubConnection.SendAsync("GetAnRSAPublic", partnerUsername);
            }
            else
            {
                await ReconnectAsync();
            }
        }

        public async Task ReconnectAsync()
        {
            await DisconnectAsync();
            await ConnectAsync();
        }

        public async Task SendMessage(Message message)
        {
            if (hubConnection != null)
            {
                await hubConnection.SendAsync("Dispatch", message);
            }
            else
            {
                await ReconnectAsync();
                await SendMessage(message);
            }
        }
        public async Task SendUserMessage(string text, string targetGroup, string myUsername)
        {
            Guid messageId = Guid.NewGuid();
            Message messageToSend = await _messageBuilder.BuildMessageToBeSend(text, targetGroup, myUsername, messageId);

            await AddAsUnreceived(text, targetGroup, myUsername, messageId);

            await AddToMessageBox(text, targetGroup, myUsername, messageId);

            await SendMessage(messageToSend);
        }

        private async Task AddToMessageBox(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _messageBox.AddMessageAsync(new Message
            {
                Id = messageId,
                Sender = myUsername,
                TargetGroup = targetGroup,
                PlainTextPayload = text,
                DateSent = DateTime.UtcNow
            },
            isEncrypted: false);
        }

        private async Task AddAsUnreceived(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _undeliveredMessagesRepository.AddAsync(new Message
            {
                Id = messageId,
                Sender = myUsername,
                TargetGroup = targetGroup,
                PlainTextPayload = text,
                DateSent = DateTime.UtcNow
            });
        }
    }
}
