using System.Text.Json;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.ClientOnlyModels.ClientOnlyExtentions;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDecryption;
using Ethachat.Client.Services.ContactsProvider;
using Ethachat.Client.Services.HubServices.HubServices.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using Ethachat.Client.Services.KeyStorageService.Implementations;
using Ethachat.Client.UI.Chat.Logic.MessageBuilder;
using EthachatShared.Constants;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.EventNameConstants;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using InMemoryKeyStorage = Ethachat.Client.Cryptography.KeyStorage.InMemoryKeyStorage;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly IMessageBox _messageBox;
        private readonly ICryptographyService _cryptographyService;
        private readonly IUsersService _usersService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IMessageDecryptor _messageDecryptor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IConfiguration _configuration;
        private readonly IBinarySendingManager _binarySendingManager;
        private readonly IBinaryReceivingManager _binaryReceivingManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly IAesTransmissionManager _aesTransmissionManager;
        private readonly IContactsProvider _contactsProvider;
        private bool _isConnectionClosedCallbackSet = false;
        private string myName;
        private bool IsRoutinesCompleted => !string.IsNullOrWhiteSpace(myName);
        private AckMessageBuilder AckMessageBuilder => new();
        private HubConnection? hubConnection { get; set; }

        public MessageService
        (IMessageBox messageBox,
            NavigationManager navigationManager,
            ICryptographyService cryptographyService,
            IUsersService usersService,
            ICallbackExecutor callbackExecutor,
            IMessageBuilder messageBuilder,
            IMessageDecryptor messageDecryptor,
            IAuthenticationHandler authenticationHandler,
            IConfiguration configuration,
            IBinarySendingManager binarySendingManager,
            IBinaryReceivingManager binaryReceivingManager,
            IJSRuntime jsRuntime,
            IAesTransmissionManager aesTransmissionManager,
            IContactsProvider contactsProvider)
        {
            _messageBox = messageBox;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _messageBuilder = messageBuilder;
            _messageDecryptor = messageDecryptor;
            _authenticationHandler = authenticationHandler;
            _configuration = configuration;
            _binarySendingManager = binarySendingManager;
            _binaryReceivingManager = binaryReceivingManager;
            _jsRuntime = jsRuntime;
            _aesTransmissionManager = aesTransmissionManager;
            _contactsProvider = contactsProvider;
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

            if (hubConnection == null)
                throw new ArgumentException($"{nameof(hubConnection)} was not properly instantiated.");

            while (hubConnection.State is HubConnectionState.Disconnected)
            {
                try
                {
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
                throw new NullReferenceException("Could not register event handlers - hub was null.");

            hubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            hubConnection.On<Guid>(SystemEventType.MessageRegisteredByHub.ToString(),
                messageId => { _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageRegisteredByHub"); });

            hubConnection.On<Guid, int>("PackageRegisteredByHub", (fileId, packageIndex) =>
                _binarySendingManager.HandlePackageRegisteredByHub(fileId, packageIndex));

            hubConnection.On<AuthResult>("OnAccessTokenInvalid",
                authResult => { NavigationManager.NavigateTo("signin"); });

            hubConnection.On<Guid>("MetadataRegisteredByHub", metadataId => { });

            hubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    await (await GetHubConnectionAsync())
                        .SendAsync("OnAck", AckMessageBuilder.CreateMessageAck(message));

                    if (_messageBox.Contains(message))
                        return;

                    if (message.Type is MessageType.RsaPubKey)
                    {
                        InMemoryKeyStorage.RSAKeyStorage.TryGetValue(message.Sender, out var rsaKey);
                        if (rsaKey?.Value?.ToString() == message.Cryptogramm?.Cyphertext && !string.IsNullOrWhiteSpace(rsaKey?.Value?.ToString()))
                            return;
                        
                        //Storing Public Key in our in-memory storage
                        InMemoryKeyStorage.RSAKeyStorage.TryAdd(message.Sender, new Key
                        {
                            Type = KeyType.RsaPublic,
                            Contact = message.Sender,
                            Format = KeyFormat.PemSpki,
                            Value = message.Cryptogramm!.Cyphertext
                        });
                        await RegenerateAESAsync(_cryptographyService, message.Sender, message.Cryptogramm.Cyphertext);
                    }

                    if (message.Type is MessageType.MessageReadConfirmation)
                    {
                        var decryptedMessageIdData =
                            await _cryptographyService.DecryptAsync<AESHandler>(message.Cryptogramm!, message.Sender);
                        _callbackExecutor.ExecuteSubscriptionsByName(Guid.Parse(decryptedMessageIdData.Cyphertext!),
                            "OnReceiverMarkedMessageAsRead");
                    }

                    if (message.Type is MessageType.MessageReceivedConfirmation)
                    {
                        var decryptedMessageIdData =
                            await _cryptographyService.DecryptAsync<AESHandler>(message.Cryptogramm!, message.Sender);
                        _callbackExecutor.ExecuteSubscriptionsByName(Guid.Parse(decryptedMessageIdData.Cyphertext!),
                            "OnReceiverMarkedMessageAsReceived");
                    }

                    if (message.Type is MessageType.TextMessage || message.Type is MessageType.HLSPlaylist)
                    {
                        await SendReceivedConfirmation(message.Id, message.Sender!);
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
                            {
                                if (message.Type is MessageType.HLSPlaylist)
                                {
                                    clientMessage.HlsPlaylist = JsonSerializer
                                        .Deserialize<HlsPlaylist>(decryptedMessageCryptogramm.Cyphertext);
                                }
                                else
                                {
                                    clientMessage.PlainText = decryptedMessageCryptogramm.Cyphertext;
                                }
                            }

                            _messageBox.AddMessage(clientMessage);
                        }
                        else
                        {
                            if (message.Sender is not null)
                                await NegotiateOnAESAsync(message.Sender);
                        }
                    }
                    else if (message.Type is MessageType.Metadata || message.Type is MessageType.DataPackage)
                    {
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnBinaryTransmitting");

                        (bool isTransmissionCompleted, Guid fileId) progressStatus =
                            await _binaryReceivingManager.StoreAsync(message);

                        if (progressStatus.isTransmissionCompleted)
                        {
                            await NotifyAboutSuccessfullDataTransfer(progressStatus.fileId,
                                message.Sender ?? throw new ArgumentException($"Invalid {message.Sender}"));
                        }
                    }
                    else if (message.Type == MessageType.AesOfferAccept)
                    {
                        if (string.IsNullOrWhiteSpace(message.Sender)
                            || message.Cryptogramm?.KeyId == Guid.Empty
                            || message.Cryptogramm?.KeyId == null)
                            throw new ArgumentException("Invalid offer accept message");
                        
                        await MarkKeyAsAccepted(message.Cryptogramm.KeyId, message.Sender);
                        
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
                        _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        await MarkContactAsTrusted(message.Sender!);
                    }
                    else if (message.Type == MessageType.AesOffer)
                    {
                        var offerResponse = await _aesTransmissionManager.GenerateOfferResponse(message);
                        await MarkContactAsTrusted(message.Sender!);
                        await hubConnection.SendAsync("Dispatch", offerResponse);

                        if (offerResponse.Type is MessageType.AesOfferAccept)
                            _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                    }
                }
            });

            hubConnection.On<string>("OnMyNameResolved", async username =>
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

            hubConnection.On<Guid>("OnFileTransfered",
                messageId => { _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnFileReceived"); });

            hubConnection.On<string>("OnConversationDeleteRequest",
                partnerName => { _messageBox.Delete(partnerName); });

            hubConnection.On<string>("OnTyping",
                (partnerName) => { _callbackExecutor.ExecuteSubscriptionsByName(partnerName, "OnTyping"); });
        }

        private async Task MarkKeyAsAccepted(Guid keyId, string contact)
        {
            var keyStorageService = new LocalStorageKeyStorage(_jsRuntime);
            var keys = await keyStorageService.GetAsync(contact, KeyType.Aes);
            var acceptedKeyId = keyId;

            var acceptedKey = keys.First(x => x.Id == acceptedKeyId);
            if (acceptedKey.IsAccepted)
                return;
                        
            acceptedKey.IsAccepted = true;
            await keyStorageService.UpdateAsync(acceptedKey);
        }

        private async Task NotifyAboutSuccessfullDataTransfer(Guid dataFileId, string sender)
        {
            if (hubConnection != null && hubConnection.State is HubConnectionState.Connected)
            {
                try
                {
                    await hubConnection.SendAsync("OnDataTranferSuccess", dataFileId, sender);
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
                await NotifyAboutSuccessfullDataTransfer(dataFileId, sender);
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
            var aesKey = await cryptographyService.GenerateAesKeyAsync(partnersUsername);
            
            var offer = await _aesTransmissionManager.GenerateOffer(partnersUsername, partnersPublicKey, aesKey);
            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("Dispatch", offer);
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            var connection = await GetHubConnectionAsync();

            await connection.SendAsync("GetAnRSAPublic", partnerUsername, await _authenticationHandler.GetUsernameAsync());
        }

        public async Task SendTypingEventToPartnerAsync(string sender, string receiver)
        {
            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("OnTyping", sender, receiver);
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
                case MessageType.HLSPlaylist:
                    await SendHlsPlaylist(message);
                    break;
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

        private async Task SendHlsPlaylist(ClientMessage message)
        {
            var messageToSend = new Message()
            {
                Id = message.Id,
                Cryptogramm = await _cryptographyService
                    .EncryptAsync<AESHandler>(new Cryptogramm
                    {
                        Cyphertext = JsonSerializer.Serialize(message.HlsPlaylist),
                    }, contact: message.TargetGroup),
                TargetGroup = message.TargetGroup,
                DateSent = DateTime.UtcNow,
                Type = MessageType.HLSPlaylist,
                Sender = myName
            };

            AddToMessageBox(message);

            await (await GetHubConnectionAsync()).SendAsync("Dispatch", messageToSend);
        }

        private async Task SendText(ClientMessage message)
        {
            var myUsername = await _authenticationHandler.GetUsernameAsync();
            Guid messageId = Guid.NewGuid();
            Message messageToSend =
                await _messageBuilder.BuildMessageToBeSend(message.PlainText, message.TargetGroup, myUsername,
                    messageId, MessageType.TextMessage);

            await AddToMessageBox(message.PlainText, message.TargetGroup, myUsername, messageId);

            var connection = await GetHubConnectionAsync();

            await connection.SendAsync("Dispatch", messageToSend);
        }

        public async Task RequestPartnerToDeleteConvertation(string targetGroup)
        {
            await GetHubConnectionAsync();
            await hubConnection!.SendAsync("DeleteConversation", myName, targetGroup);
        }

        private void AddToMessageBox(ClientMessage message)
        {
            _messageBox.AddMessage(message);
        }

        private async Task AddToMessageBox(string text, string targetGroup, string myUsername, Guid messageId)
        {
            _messageBox.AddMessage(new ClientMessage
            {
                Id = messageId,
                Sender = myUsername,
                TargetGroup = targetGroup,
                PlainText = text,
                DateSent = DateTime.UtcNow,
                Type = MessageType.TextMessage
            });
        }

        public async Task SendReceivedConfirmation(Guid messageId, string messageSender)
        {
            var myUsername = await _authenticationHandler.GetUsernameAsync();

            if (messageSender == myUsername)
                return;

            var connection = await GetHubConnectionAsync();

            var encryptedMessageIdData = await _cryptographyService.EncryptAsync<AESHandler>(new Cryptogramm()
            {
                Cyphertext = messageId.ToString()
            }, messageSender);

            await connection.SendAsync("Dispatch", new Message
            {
                Sender = myUsername,
                TargetGroup = messageSender,
                Cryptogramm = encryptedMessageIdData,
                Type = MessageType.MessageReceivedConfirmation
            });
        }

        public async Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername)
        {
            if (messageSender == myUsername)
                return;

            var connection = await GetHubConnectionAsync();

            var encryptedMessageIdData = await _cryptographyService.EncryptAsync<AESHandler>(new Cryptogramm()
            {
                Cyphertext = messageId.ToString()
            }, messageSender);
            await connection.SendAsync("Dispatch", new Message
            {
                Type = MessageType.MessageReadConfirmation,
                TargetGroup = messageSender,
                Sender = myName,
                Cryptogramm = encryptedMessageIdData
            });
        }


        private async Task MarkContactAsTrusted(string contactUsername)
        {
            var contact = await _contactsProvider.GetContact(contactUsername, _jsRuntime);
            if (contact is not null && !string.IsNullOrWhiteSpace(contact.TrustedPassphrase))
            {
                contact.IsTrusted = true;
                await _contactsProvider.UpdateContact(contact, _jsRuntime);
            }
        }
    }
}