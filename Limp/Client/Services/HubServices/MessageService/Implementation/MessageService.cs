using ClientServerCommon.Models;
using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling;
using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.HubService.UsersService;
using Limp.Client.Services.HubServices.CommonServices;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.InboxService;
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
        ICallbackExecutor callbackExecutor)
        {
            _messageBox = messageBox;
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
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
            hubConnection.On<List<UserConnection>>("ReceiveOnlineUsers", updatedTrackedUserConnections =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
            });

            hubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
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

                await hubConnection.SendAsync("MessageReceived", message.Id);

                //If we dont yet know a partner Public Key, we will request it from server side.
                if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == message.Sender).Value == null)
                {
                    await hubConnection.SendAsync("GetAnRSAPublic", message.Sender);
                }
            });

            hubConnection.On<Guid>("MessageWasReceivedByRecepient", messageId =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageWasReceivedByRecepient");
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
                InMemoryKeyStorage.AESKeyStorage.First(x=>x.Key == partnersUsername).Value.CreationDate = DateTime.UtcNow;
                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value = aesKeyForConversation;
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
    }
}
