using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Cryptography.KeyStorage;
using Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling;
using Ethachat.Client.Pages.Chat.Logic.MessageBuilder;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.ClientOnlyModels.ClientOnlyExtentions;
using Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDecryption;
using Ethachat.Client.Services.BrowserKeyStorageService;
using Ethachat.Client.Services.HubServices.CommonServices.HubServiceConnectionBuilder;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinarySending;
using EthachatShared.Constants;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly IMessageBox _messageBox;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAESOfferHandler _aESOfferHandler;
        private readonly IUsersService _usersService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IBrowserKeyStorage _browserKeyStorage;
        private readonly IMessageDecryptor _messageDecryptor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IConfiguration _configuration;
        private readonly IBinarySendingManager _binarySendingManager;
        private readonly IBinaryReceivingManager _binaryReceivingManager;
        private readonly IJSRuntime _jsRuntime;
        private bool _isConnectionClosedCallbackSet = false;
        private string myName;
        public bool IsConnected() => hubConnection?.State == HubConnectionState.Connected;
        private bool IsRoutinesCompleted => !string.IsNullOrWhiteSpace(myName);

        private HubConnection? hubConnection { get; set; }

        public MessageService
        (IMessageBox messageBox,
            NavigationManager navigationManager,
            ICryptographyService cryptographyService,
            IAESOfferHandler aESOfferHandler,
            IUsersService usersService,
            ICallbackExecutor callbackExecutor,
            IMessageBuilder messageBuilder,
            IBrowserKeyStorage browserKeyStorage,
            IMessageDecryptor messageDecryptor,
            IAuthenticationHandler authenticationHandler,
            IConfiguration configuration,
            IBinarySendingManager binarySendingManager,
            IBinaryReceivingManager binaryReceivingManager,
            IJSRuntime jsRuntime)
        {
            _messageBox = messageBox;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _messageBuilder = messageBuilder;
            _browserKeyStorage = browserKeyStorage;
            _messageDecryptor = messageDecryptor;
            _authenticationHandler = authenticationHandler;
            _configuration = configuration;
            _binarySendingManager = binarySendingManager;
            _binaryReceivingManager = binaryReceivingManager;
            _jsRuntime = jsRuntime;
            InitializeHubConnection();
            RegisterHubEventHandlers();
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            //Shortcut connection is alive and ready to be used
            if (hubConnection?.State is HubConnectionState.Connected && IsRoutinesCompleted)
                return hubConnection;
            
            if (!await _authenticationHandler.IsSetToUseAsync())
            {
                NavigationManager.NavigateTo("signin");
                return null;
            }

            //Loading from local storage earlier saved AES keys
            InMemoryKeyStorage.AESKeyStorage =
                (await _browserKeyStorage.ReadLocalKeyChainAsync())?.AESKeyStorage ?? new();

            if (hubConnection == null)
                throw new ArgumentException($"{nameof(hubConnection)} was not properly instantiated.");

            while (hubConnection.State is not HubConnectionState.Connected)
            {
                try
                {
                    if (hubConnection.State is not HubConnectionState.Disconnected)
                        await hubConnection.StopAsync();

                    await hubConnection.StartAsync();
                }
                catch
                {
                    var interval = int.Parse(_configuration["HubConnection:ReconnectionIntervalMs"] ?? "0");
                    await Task.Delay(interval);
                    return await GetHubConnectionAsync();
                }
            }
            
            await hubConnection.SendAsync("SetUsername", await _authenticationHandler.GetCredentialsDto());
            
            _callbackExecutor.ExecuteSubscriptionsByName(true, "OnMessageHubConnectionStatusChanged");

            if (_isConnectionClosedCallbackSet is false)
            {
                hubConnection.Closed += OnConnectionLost;
                _isConnectionClosedCallbackSet = true;
            }

            return hubConnection;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(false, "OnMessageHubConnectionStatusChanged");
            await GetHubConnectionAsync();
        }

        private void InitializeHubConnection()
        {
            if (hubConnection is not null)
                return;
            
            hubConnection = HubServiceConnectionBuilder
                .Build(NavigationManager.ToAbsoluteUri(HubRelativeAddresses.MessageHubRelativeAddress));
        }

        private void RegisterHubEventHandlers()
        {
            if (hubConnection is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            hubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            hubConnection.On<Guid>("MessageRegisteredByHub",
                messageId => _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageRegisteredByHub"));

            hubConnection.On<Guid, int>("PackageRegisteredByHub", (fileId, packageIndex) =>
                _binarySendingManager.HandlePackageRegisteredByHub(fileId, packageIndex));

            hubConnection.On<AuthResult>("OnAccessTokenInvalid", (authResult) =>
            {
                Console.WriteLine(authResult.Message);
            });

            hubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    if (hubConnection.State != HubConnectionState.Connected)
                    {
                        await hubConnection.StopAsync();
                        await hubConnection.StartAsync();
                    }

                    if (message.Type is MessageType.TextMessage)
                    {
                        if (string.IsNullOrWhiteSpace(message.Sender))
                            throw new ArgumentException(
                                $"Cannot get a message sender - {nameof(message.Sender)} contains empty string.");

                        Cryptogramm decryptedMessageCryptogramm = new Cryptogramm();
                        try
                        {
                            decryptedMessageCryptogramm = await _messageDecryptor.DecryptAsync(message);
                        }
                        catch (Exception ex)
                        {
                            await Console.Out.WriteLineAsync(
                                "Cannot decrypt received user message. Regenerating AES key.");
                            await NegotiateOnAESAsync(message.Sender);
                        }

                        if (!string.IsNullOrWhiteSpace(decryptedMessageCryptogramm.Cyphertext))
                        {
                            ClientMessage clientMessage = message.AsClientMessage();

                            if (!string.IsNullOrWhiteSpace(decryptedMessageCryptogramm.Cyphertext))
                                clientMessage.PlainText = decryptedMessageCryptogramm.Cyphertext;

                            await hubConnection.SendAsync("MessageReceived", message.Id, message.Sender);
                            await _messageBox.AddMessageAsync(clientMessage, false);
                        }
                        else
                        {
                            if (message.Sender is not null)
                                await NegotiateOnAESAsync(message.Sender);
                        }
                    }
                    else if (message.Type == MessageType.Metadata)
                    {
                        _binaryReceivingManager.StoreMetadata(message.Metadata);
                    }
                    else if (message.Type == MessageType.DataPackage)
                    {
                        var decryptedB64 = await _cryptographyService.DecryptAsync<AESHandler>(new()
                        {
                            Cyphertext = message.Package.B64Data,
                            Iv = message.Package.IV
                        }, message.Sender);

                        var isAllPackagesAreLoaded = _binaryReceivingManager.StoreFile(message.Package.FileDataid, new()
                        {
                            Index = message.Package.Index,
                            PlainB64Data = decryptedB64.Cyphertext,
                            FileDataid = message.Package.FileDataid
                        });

                        if (isAllPackagesAreLoaded)
                        {
                            var metadata = _binaryReceivingManager.GetMetadata(message.Package.FileDataid);
                            var packages = _binaryReceivingManager.GetData(message.Package.FileDataid);
                            var data = packages
                                .SelectMany(x => Convert.FromBase64String(x.PlainB64Data))
                                .ToArray();
                            
                            var blobUrl = await _jsRuntime.InvokeAsync<string>("createBlobUrl", data, metadata!.ContentType);
        
                            await _messageBox.AddMessageAsync(new ClientMessage()
                            {
                                BlobLink = blobUrl,
                                Id = metadata.DataFileId,
                                Type = MessageType.BlobLink,
                                TargetGroup = message.TargetGroup,
                                Sender = message.Sender,
                                Metadata = metadata
                            }, isEncrypted: false);
                            await NotifyAboutSuccessfullDataTransfer(message);
                        }
                    }
                    else if (message.Type == MessageType.AesAccept)
                    {
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
                        _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        InMemoryKeyStorage.AESKeyStorage[message.Sender!].IsAccepted = true;
                        return;
                    }
                    else if (message.Type == MessageType.AesOffer)
                    {
                        if (hubConnection != null)
                        {
                            await hubConnection.SendAsync("Dispatch",
                                await _aESOfferHandler.GetAESOfferResponse(message));
                            _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        }
                    }
                }

                //If we dont yet know a partner Public Key and we dont have an AES Key for chat with partner,
                //we will request it from server side.
                if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == message.Sender).Value == null
                    &&
                    _browserKeyStorage.GetAESKeyForChat(message.Sender!) == null)
                {
                    if (hubConnection == null)
                    {
                        await ReconnectAsync();
                    }
                    else
                        await hubConnection.SendAsync("GetAnRSAPublic", message.Sender);
                }
            });

            hubConnection.On<Guid>("OnReceiverMarkedMessageAsReceived",
                messageId =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnReceiverMarkedMessageAsReceived");
                });

            hubConnection.On<Guid>("MessageHasBeenRead",
                messageId =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnReceiverMarkedMessageAsRead");
                });

            //Handling server side response on partners Public Key
            hubConnection.On<string, string>("ReceivePublicKey", async (partnersUsername, partnersPublicKey) =>
            {
                if (partnersUsername == "You")
                    return;
                //Storing Public Key in our in-memory storage
                InMemoryKeyStorage.RSAKeyStorage.TryAdd(partnersUsername, new Key
                {
                    Type = KeyType.RsaPublic,
                    Contact = partnersUsername,
                    Format = KeyFormat.PemSpki,
                    Value = partnersPublicKey
                });

                //Now we can send an encrypted offer on AES Key
                //We will encrypt our offer with a partners RSA Public Key
                await RegenerateAESAsync(_cryptographyService!, partnersUsername, partnersPublicKey);
            });

            hubConnection.On<string>("OnMyNameResolve", async username =>
            {
                myName = username;
                if (!await _authenticationHandler.IsSetToUseAsync())
                {
                    NavigationManager.NavigateTo("/signIn");
                    return;
                }

                if (string.IsNullOrWhiteSpace(InMemoryKeyStorage.MyRSAPublic?.Value?.ToString()))
                {
                    throw new ApplicationException("RSA Public key was not properly generated.");
                }

                await UpdateRSAPublicKeyAsync(InMemoryKeyStorage.MyRSAPublic);
            });

            hubConnection.On<Guid>("OnFileTransfered", messageId =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnFileReceived");
            });

            hubConnection.On<string>("OnConvertationDeleteRequest",
                partnerName => { _messageBox.Delete(partnerName); });
        }

        private async Task NotifyAboutSuccessfullDataTransfer(Message message)
        {
            if (hubConnection != null && hubConnection.State is HubConnectionState.Connected)
            {
                try
                {
                    await hubConnection.SendAsync("OnDataTranferSuccess", message.Id, message.Sender);
                }
                catch (Exception e)
                {
                    throw new ApplicationException
                        ($"{nameof(MessageService)}.{nameof(SendMessage)}: {e.Message}.");
                }
            }
            else
            {
                await GetHubConnectionAsync();
                await NotifyAboutSuccessfullDataTransfer(message);
            }
        }

        public async Task UpdateRSAPublicKeyAsync(Key RSAPublicKey)
        {
            if (!InMemoryKeyStorage.isPublicKeySet)
            {
                await _usersService.SetRSAPublicKey(RSAPublicKey);
            }
        }

        private async Task RegenerateAESAsync
        (ICryptographyService cryptographyService,
            string partnersUsername,
            string partnersPublicKey)
        {
            await cryptographyService.GenerateAesKeyAsync(partnersUsername, async (aesKeyForConversation) =>
            {
                if (hubConnection?.State is not HubConnectionState.Connected)
                {
                    await GetHubConnectionAsync();
                    while (hubConnection?.State is not HubConnectionState.Connected)
                    {
                        await GetHubConnectionAsync();
                    }
                }

                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.CreationDate =
                    DateTime.UtcNow;
                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value =
                    aesKeyForConversation;
                await _browserKeyStorage.SaveInMemoryKeysInLocalStorage();
                string? offeredAESKeyForConversation =
                    InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value!.ToString();

                if (string.IsNullOrWhiteSpace(offeredAESKeyForConversation))
                    throw new ApplicationException("Could not properly generated an AES Key for conversation");

                //When this callback is called, AES key for conversation is already generated
                //We now need to encrypt this AES key and send it to partner
                string? encryptedAESKey = (await cryptographyService
                    .EncryptAsync<RSAHandler>
                    (new Cryptogramm { Cyphertext = offeredAESKeyForConversation },
                        //We will encrypt it with partners Public Key, so he will be able to decrypt it with his Private Key
                        publicKeyToEncryptWith: partnersPublicKey)).Cyphertext;

                if (string.IsNullOrWhiteSpace(encryptedAESKey))
                    throw new ApplicationException("Could not encrypt a AES Key, got empty string.");

                Message offerOnAES = new()
                {
                    Type = MessageType.AesOffer,
                    DateSent = DateTime.UtcNow,
                    Sender = await _authenticationHandler.GetUsernameAsync(),
                    TargetGroup = partnersUsername,
                    Cryptogramm = new()
                    {
                        Cyphertext = encryptedAESKey,
                    }
                };

                if (hubConnection.State is not HubConnectionState.Connected)
                {
                    await GetHubConnectionAsync();
                }

                await hubConnection!.SendAsync("Dispatch", offerOnAES);
            });
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            if (hubConnection?.State is not HubConnectionState.Connected)
            {
                if (hubConnection is not null)
                    await hubConnection.StopAsync();

                await GetHubConnectionAsync();
                await NegotiateOnAESAsync(partnerUsername);
                return;
            }

            await hubConnection.SendAsync("GetAnRSAPublic", partnerUsername);
        }

        public async Task ReconnectAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.StopAsync();
                await hubConnection.DisposeAsync();
            }

            await GetHubConnectionAsync();
        }

        public async Task SendMessage(ClientMessage message)
        {
            switch (message.Type)
            {
                case MessageType.TextMessage:
                    await SendText(message);
                    break;
                case MessageType.Metadata:
                    await _binarySendingManager.SendMetadata(message, GetHubConnectionAsync);
                    break;
                case MessageType.BrowserFileMessage:
                    await _binarySendingManager.SendFile(message, GetHubConnectionAsync);
                    break;
                default:
                    throw new ArgumentException($"Unhandled message type passed: {message.Type}.");
            }
        }

        private async Task SendText(ClientMessage message)
        {
            var myUsername = await _authenticationHandler.GetUsernameAsync();
            Guid messageId = Guid.NewGuid();
            Message messageToSend =
                await _messageBuilder.BuildMessageToBeSend(message.PlainText, message.TargetGroup, myUsername, messageId, MessageType.TextMessage);

            await AddToMessageBox(message.PlainText, message.TargetGroup, myUsername, messageId);
            
            if (hubConnection?.State is not HubConnectionState.Connected)
                hubConnection = await GetHubConnectionAsync();

            await hubConnection.SendAsync("Dispatch", messageToSend);
        }

        public async Task RequestPartnerToDeleteConvertation(string targetGroup)
        {
            await GetHubConnectionAsync();
            await hubConnection!.SendAsync("DeleteConversation", myName, targetGroup);
        }

        private async Task AddToMessageBox(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _messageBox.AddMessageAsync(new ClientMessage
                {
                    Id = messageId,
                    Sender = myUsername,
                    TargetGroup = targetGroup,
                    PlainText = text,
                    DateSent = DateTime.UtcNow,
                    Type = MessageType.TextMessage
                },
                isEncrypted: false);
        }

        public async Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername)
        {
            if (messageSender ==
                myUsername) //If it's our message, we don't want to notify partner that we've seen our message
                return;

            if (hubConnection is not null)
            {
                if (hubConnection?.State is not HubConnectionState.Connected)
                {
                    if (hubConnection is not null)
                        await hubConnection.StopAsync();

                    await GetHubConnectionAsync();
                    await NotifySenderThatMessageWasReaded(messageId, messageSender, myUsername);
                    return;
                }

                if (hubConnection.State is HubConnectionState.Connected)
                {
                    await hubConnection.SendAsync("MessageHasBeenRead", messageId, messageSender);
                }
            }

            throw new ArgumentException("Notification was not sent because hub connection is lost.");
        }
    }
}