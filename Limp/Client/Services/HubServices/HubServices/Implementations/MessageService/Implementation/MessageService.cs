using System.Reflection;
using System.Text.Json;
using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.Services.ContactsProvider;
using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService;
using Ethachat.Client.Services.HubServices.HubServices.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing;
using
    Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.UndecryptedMessagesService;
using Ethachat.Client.Services.UndecryptedMessagesService.Models;
using Ethachat.Client.UI.Chat.Logic.MessageBuilder;
using EthachatShared.Constants;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.EventNameConstants;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        private NavigationManager NavigationManager { get; set; }
        private readonly IMessageBox _messageBox;
        private readonly ICryptographyService _cryptographyService;
        private readonly IUsersService _usersService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IConfiguration _configuration;
        private readonly IBinarySendingManager _binarySendingManager;
        private readonly IBinaryReceivingManager _binaryReceivingManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly IAesTransmissionManager _aesTransmissionManager;
        private readonly IContactsProvider _contactsProvider;
        private readonly IKeyStorage _keyStorage;
        private bool _isConnectionClosedCallbackSet = false;
        private AckMessageBuilder AckMessageBuilder => new();
        private HubConnection? HubConnection { get; set; }
        private MessageProcessor _messageProcessor;

        public MessageService
        (IMessageBox messageBox,
            NavigationManager navigationManager,
            ICryptographyService cryptographyService,
            IUsersService usersService,
            ICallbackExecutor callbackExecutor,
            IMessageBuilder messageBuilder,
            IAuthenticationHandler authenticationHandler,
            IConfiguration configuration,
            IBinarySendingManager binarySendingManager,
            IBinaryReceivingManager binaryReceivingManager,
            IJSRuntime jsRuntime,
            IAesTransmissionManager aesTransmissionManager,
            IContactsProvider contactsProvider,
            IHubServiceSubscriptionManager hubServiceSubscriptionManager,
            IKeyStorage keyStorage)
        {
            _messageBox = messageBox;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _messageBuilder = messageBuilder;
            _authenticationHandler = authenticationHandler;
            _configuration = configuration;
            _binarySendingManager = binarySendingManager;
            _binaryReceivingManager = binaryReceivingManager;
            _jsRuntime = jsRuntime;
            _aesTransmissionManager = aesTransmissionManager;
            _contactsProvider = contactsProvider;
            _keyStorage = keyStorage;
            InitializeHubConnection();
            RegisterHubEventHandlers();

            _ = new UndecryptedMessagesStorageService(hubServiceSubscriptionManager, this, authenticationHandler);
            
            var transferHandlerFactory = new TransferHandlerFactory();
            transferHandlerFactory.RegisterHandler(nameof(TextMessage), new TextMessageHandler(messageBox, authenticationHandler, this));
            transferHandlerFactory.RegisterHandler(MessageType.ConversationDeletionRequest.ToString(), new ConversationDeletionRequestHandler(messageBox));
            transferHandlerFactory.RegisterHandler(MessageType.MessageReadConfirmation.ToString(), new MessageReadHandler(callbackExecutor));
            _messageProcessor = new(transferHandlerFactory);
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            //Shortcut connection is alive and ready to be used
            if (HubConnection?.State is HubConnectionState.Connected
                && !string.IsNullOrWhiteSpace(await _authenticationHandler.GetUsernameAsync()))
                return HubConnection;

            if (!await _authenticationHandler.IsSetToUseAsync())
            {
                NavigationManager.NavigateTo("signin");
                return null;
            }

            if (HubConnection == null)
                throw new ArgumentException($"{nameof(HubConnection)} was not properly instantiated.");

            while (HubConnection.State is HubConnectionState.Disconnected)
            {
                try
                {
                    await HubConnection.StartAsync();
                }
                catch
                {
                    var interval = int.Parse(_configuration["HubConnection:ReconnectionIntervalMs"] ?? "0");
                    await Task.Delay(interval);
                    return await GetHubConnectionAsync();
                }
            }

            await HubConnection.SendAsync("SetUsername", await _authenticationHandler.GetCredentialsDto());

            _callbackExecutor.ExecuteSubscriptionsByName(true, "OnMessageHubConnectionStatusChanged");

            if (_isConnectionClosedCallbackSet is false)
            {
                HubConnection.Closed += OnConnectionLost;
                _isConnectionClosedCallbackSet = true;
            }

            return HubConnection;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(false, "OnMessageHubConnectionStatusChanged");
            await GetHubConnectionAsync();
        }

        private void InitializeHubConnection()
        {
            if (HubConnection is not null)
                return;

            HubConnection = HubServiceConnectionBuilder
                .Build(NavigationManager.ToAbsoluteUri(HubRelativeAddresses.MessageHubRelativeAddress));
        }

        private void RegisterHubEventHandlers()
        {
            if (HubConnection is null)
                throw new NullReferenceException("Could not register event handlers - hub was null.");

            HubConnection.On<EncryptedDataTransfer>("OnTransfer", async transfer =>
            {
                if (transfer.Sender != await _authenticationHandler.GetUsernameAsync())
                {
                    var connection = await GetHubConnectionAsync();
                    await connection.SendAsync("OnTransferAcked", transfer);
                }

                var decryptionMethodName = nameof(DecryptTransferAsync);
                MethodInfo? method = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == decryptionMethodName && m.IsGenericMethod);
                if (method != null)
                {
                    MethodInfo genericMethod = method.MakeGenericMethod(transfer.DataType);
                    Task? task = (Task?)genericMethod.Invoke(this, [transfer]);
                    if (task != null)
                    {
                        await task;
                        PropertyInfo? dataTypeProperty = task.GetType().GetProperty("Result");
                        if (dataTypeProperty != null)
                        {
                            var decryptedData = dataTypeProperty.GetValue(task, null);
                            if (transfer.DataType == typeof(TextMessage))
                            {
                                await _messageProcessor.ProcessTransferAsync(nameof(TextMessage),
                                    decryptedData ?? throw new ArgumentException());
                            }

                            if (transfer.DataType == typeof(ClientMessage))
                            {
                                var clientMessage = (ClientMessage?)decryptedData;
                                if (clientMessage is null)
                                    throw new ArgumentException("Could not convert data transfer to target type");

                                if (clientMessage.Type is MessageType.ResendRequest)
                                {
                                    var message = _messageBox.Messages.FirstOrDefault(x => x.Id == clientMessage.Id);
                                    if (message?.Type is MessageType.TextMessage)
                                        await SendText(message);
                                }

                                if (clientMessage.Type == MessageType.MessageReceivedConfirmation)
                                    _callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Id,
                                        "OnReceiverMarkedMessageAsReceived");

                                if (clientMessage.Type == MessageType.MessageReadConfirmation)
                                {
                                    await _messageProcessor.ProcessTransferAsync(clientMessage.Type.ToString(),
                                        decryptedData ?? throw new ArgumentException());
                                }

                                if (clientMessage.Type == MessageType.HLSPlaylist)
                                {
                                    _messageBox.AddMessage(clientMessage);
                                }

                                if (clientMessage.Type is MessageType.Metadata ||
                                    clientMessage.Type is MessageType.DataPackage)
                                {
                                    _callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Sender,
                                        "OnBinaryTransmitting");

                                    (bool isTransmissionCompleted, Guid fileId) progressStatus =
                                        await _binaryReceivingManager.StoreAsync(clientMessage);

                                    if (progressStatus.isTransmissionCompleted)
                                    {
                                        await NotifyAboutSuccessfullDataTransfer(progressStatus.fileId,
                                            clientMessage.Sender ??
                                            throw new ArgumentException($"Invalid {clientMessage.Sender}"));
                                    }
                                }

                                if (clientMessage.Type is MessageType.ConversationDeletionRequest)
                                {
                                    await _messageProcessor.ProcessTransferAsync(clientMessage.Type.ToString(),
                                        decryptedData ?? throw new ArgumentException());
                                }
                            }
                        }
                    }
                }
            });

            HubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            HubConnection.On<Guid>(SystemEventType.MessageRegisteredByHub.ToString(),
                messageId => { _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageRegisteredByHub"); });

            HubConnection.On<Guid, int>("PackageRegisteredByHub", (fileId, packageIndex) =>
                _binarySendingManager.HandlePackageRegisteredByHub(fileId, packageIndex));

            HubConnection.On<AuthResult>("OnAccessTokenInvalid",
                authResult => { NavigationManager.NavigateTo("signin"); });

            HubConnection.On<Guid>("MetadataRegisteredByHub", metadataId => { });

            HubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    await (await GetHubConnectionAsync())
                        .SendAsync("OnAck", AckMessageBuilder.CreateMessageAck(message));

                    if (_messageBox.Contains(message))
                        return;

                    if (message.Type is MessageType.RsaPubKey)
                    {
                        await _keyStorage.StoreAsync(new Key
                        {
                            Type = KeyType.RsaPublic,
                            Contact = message.Sender,
                            Format = KeyFormat.PemSpki,
                            Value = message.Cryptogramm!.Cyphertext
                        });

                        await RegenerateAESAsync(_cryptographyService, message.Sender, message.Cryptogramm.Cyphertext);
                    }

                    if (message.Type == MessageType.AesOfferAccept)
                    {
                        if (string.IsNullOrWhiteSpace(message.Sender)
                            || message.Cryptogramm?.KeyId == Guid.Empty
                            || message.Cryptogramm?.KeyId == null)
                            throw new ArgumentException("Invalid offer accept message");

                        await MarkKeyAsAccepted(message.Cryptogramm.KeyId, message.Sender);

                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "AESUpdated");
                        await MarkContactAsTrusted(message.Sender!);
                    }
                    else if (message.Type == MessageType.AesOffer)
                    {
                        var offerResponse = await _aesTransmissionManager.GenerateOfferResponse(message);
                        await MarkContactAsTrusted(message.Sender!);
                        await HubConnection.SendAsync("Dispatch", offerResponse);

                        if (offerResponse.Type is MessageType.AesOfferAccept)
                            _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "AESUpdated");
                    }
                }
            });

            HubConnection.On<string>("OnMyNameResolved", async username =>
            {
                if (!await _authenticationHandler.IsSetToUseAsync())
                {
                    NavigationManager.NavigateTo("/signIn");
                    return;
                }

                var rsaPublicKey = await _keyStorage.GetAsync(string.Empty, KeyType.RsaPublic);
                if (string.IsNullOrWhiteSpace(rsaPublicKey.First().Value?.ToString()))
                {
                    throw new ApplicationException("RSA Public key was not properly generated.");
                }

                await UpdateRSAPublicKeyAsync(rsaPublicKey.First());
            });

            HubConnection.On<Guid>("OnFileTransfered",
                messageId => { _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnFileReceived"); });

            HubConnection.On<string>("OnTyping",
                (partnerName) => { _callbackExecutor.ExecuteSubscriptionsByName(partnerName, "OnTyping"); });
        }

        private async Task MarkKeyAsAccepted(Guid keyId, string contact)
        {
            var keys = await _keyStorage.GetAsync(contact, KeyType.Aes);
            var acceptedKeyId = keyId;

            var acceptedKey = keys.First(x => x.Id == acceptedKeyId);
            if (acceptedKey.IsAccepted)
                return;

            acceptedKey.IsAccepted = true;
            await _keyStorage.UpdateAsync(acceptedKey);
        }

        private async Task NotifyAboutSuccessfullDataTransfer(Guid dataFileId, string sender)
        {
            if (HubConnection != null && HubConnection.State is HubConnectionState.Connected)
            {
                try
                {
                    await HubConnection.SendAsync("OnDataTranferSuccess", dataFileId, sender);
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
            await _usersService.SetRSAPublicKey(RSAPublicKey);
        }

        private async Task RegenerateAESAsync
        (ICryptographyService cryptographyService,
            string partnersUsername,
            string partnersPublicKey)
        {
            var aesKey = await cryptographyService.GenerateAesKeyAsync(partnersUsername);
            aesKey.Author = await _authenticationHandler.GetUsernameAsync();

            var offer = await _aesTransmissionManager.GenerateOffer(partnersUsername, partnersPublicKey, aesKey);
            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("Dispatch", offer);
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            var connection = await GetHubConnectionAsync();

            await connection.SendAsync("GetAnRSAPublic", partnerUsername,
                await _authenticationHandler.GetUsernameAsync());
        }

        public async Task SendTypingEventToPartnerAsync(string sender, string receiver)
        {
            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("OnTyping", sender, receiver);
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
                    await SendBrowserFile(message);
                    break;
                case MessageType.ResendRequest:
                    await TransferAsync(message);
                    break;
                case MessageType.MessageReceivedConfirmation:
                    await TransferAsync(message);
                    break;
                case MessageType.ConversationDeletionRequest:
                    await TransferAsync(message);
                    break;
                default:
                    throw new ArgumentException($"Unhandled message type passed: {message.Type}.");
            }
        }

        private async Task SendHlsPlaylist(ClientMessage message)
        {
            AddToMessageBox(message);

            await TransferAsync(new ClientMessage
            {
                Id = message.Id,
                HlsPlaylist = message.HlsPlaylist,
                Target = message.Target,
                DateSent = DateTime.UtcNow,
                Type = MessageType.HLSPlaylist,
                Sender = await _authenticationHandler.GetUsernameAsync()
            });
        }

        private async Task SendText(ClientMessage message)
        {
            await AddToMessageBox(message.PlainText, message.Target, message.Id);
            await foreach (var tMessage in _messageBuilder.BuildTextMessage(message))
                await TransferAsync(tMessage, tMessage.Receiver
                                              ?? throw new ArgumentException("Target is a required parameter"));
        }

        private async Task SendBrowserFile(ClientMessage message)
        {
            await foreach (var dataPartMessage in _binarySendingManager.SendFile(message))
                await TransferAsync(dataPartMessage);
        }

        private async Task TransferAsync<T>(T data) where T : IIdentifiable, IDestinationResolvable
        {
            var serializedData = JsonSerializer.Serialize(data);

            var cryptogram = await _cryptographyService
                .EncryptAsync<AesHandler>(new Cryptogram
                    {
                        Cyphertext = serializedData,
                    },
                    await _keyStorage.GetLastAcceptedAsync(data.Target, KeyType.Aes) ??
                    throw new ApplicationException("missing key"));

            var transferData = new EncryptedDataTransfer
            {
                Id = data.Id,
                Cryptogram = cryptogram,
                Target = data.Target,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                DataType = typeof(T)
            };

            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("TransferAsync", transferData);
        }


        private async Task TransferAsync<T>(T data, string target) where T : IIdentifiable
        {
            var key = await _keyStorage.GetLastAcceptedAsync(target, KeyType.Aes) ??
                      throw new ApplicationException("Missing key");
            var serializedData = JsonSerializer.Serialize(data);

            var cryptogram = await _cryptographyService
                .EncryptAsync<AesHandler>(new Cryptogram
                {
                    Cyphertext = serializedData,
                }, key);

            var transferData = new EncryptedDataTransfer
            {
                Id = data.Id,
                Cryptogram = cryptogram,
                Target = target,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                DataType = typeof(T)
            };

            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("TransferAsync", transferData);
        }

        private async Task<T?> DecryptTransferAsync<T>(EncryptedDataTransfer dataTransfer)
        {
            try
            {
                var lastAcceptedAsync = await _keyStorage.GetLastAcceptedAsync(dataTransfer.Sender, KeyType.Aes);
                if (lastAcceptedAsync is null)
                    await NegotiateOnAESAsync(dataTransfer.Sender);

                var cryptogram = await _cryptographyService
                    .DecryptAsync<AesHandler>(dataTransfer.Cryptogram, lastAcceptedAsync);

                var json = cryptogram.Cyphertext ?? string.Empty;
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception e)
            {
                if (e.InnerException is ApplicationException keyNotFoundException)
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(new UndecryptedItem
                    {
                        Id = dataTransfer.Id,
                        Sender = dataTransfer.Sender,
                        Target = dataTransfer.Target
                    }, "OnDecryptionFailure");
                }

                Console.WriteLine(e);
                throw;
            }
        }

        private void AddToMessageBox(ClientMessage message)
        {
            _messageBox.AddMessage(message);
        }

        private async Task AddToMessageBox(string plainText, string target, Guid id)
        {
            var message = new ClientMessage
            {
                Id = id,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                Target = target,
                DateSent = DateTime.UtcNow,
                Type = MessageType.TextMessage
            };
            message.AddChunk(new()
            {
                Index = 0,
                Total = 1,
                Text = plainText
            });
            _messageBox.AddMessage(message);
        }

        public async Task NotifySenderThatMessageWasRead(Guid messageId, string messageSender, string myUsername)
        {
            if (messageSender == myUsername)
                return;

            await TransferAsync(new ClientMessage
            {
                Type = MessageType.MessageReadConfirmation,
                Target = messageSender,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                Id = messageId
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